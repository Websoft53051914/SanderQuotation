using backend.AI;
using backend.Common;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;

namespace backend.Jobs
{
    /// <summary>
    /// 內部料品表 AI 關鍵字抽取排程工作
    /// </summary>
    public class ExtractKeywordJob
    {
        /// <summary>
        /// 批次大小<para/>
        /// 若設定為 100/50 筆，有機會因為單次處理資料過多而導致 API 請求失敗
        /// </summary>
        private const int BatchSize = 30;
        /// <summary>
        /// Log 中的 Controller 名稱，方便識別是哪個工作產生的 Log
        /// </summary>
        private const string LogControllerName = nameof(ExtractKeywordJob);

        private readonly ExtractKeywordHandler _extractKeywordHandler;
        private readonly ManualBatchEmbedding _manualBatchEmbedding;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="extractKeywordHandler">AI 關鍵字抽取處理器</param>
        /// <param name="manualBatchEmbedding">向量化處理器</param>
        public ExtractKeywordJob(ExtractKeywordHandler extractKeywordHandler, ManualBatchEmbedding manualBatchEmbedding)
        {
            _extractKeywordHandler = extractKeywordHandler;
            _manualBatchEmbedding = manualBatchEmbedding;
        }

        /// <summary>
        /// 單一批次 API 失敗時的重試次數（不含首次，共嘗試 1 + 此值 次）
        /// </summary>
        private const int MaxRetryPerBatch = 2;

        /// <summary>
        /// 連續失敗（含重試用盡）批次達此數量則提前終止
        /// </summary>
        private const int MaxConsecutiveFailedBatches = 10;

        /// <summary>
        /// 執行關鍵字抽取工作
        /// 取 FlagNeedExtractKeyword = 1 的料品，每批 30 筆；
        /// 僅對 AI 有回傳關鍵字的料號寫入並更新旗標為 0，漏回傳者維持 1 供下輪重試；
        /// 單批 API 失敗時重試 MaxRetryPerBatch 次，仍失敗則標為 2（錯誤）；
        /// 連續失敗批次達 MaxConsecutiveFailedBatches 則提前終止。
        /// 工作結束後將所有 2（錯誤）重置為 1（待處理）供下次執行。
        /// </summary>
        /// <param name="logDM">排程執行紀錄，供呼叫端彙總結果；傳入 null 時略過紀錄更新</param>
        public async Task ExecuteAsync(EsScheduleCycleLogDetailDM logDM = null)
        {
            int consecutiveFailedBatchCount = 0;

            SanderModuleItemBL blSanderModuleItem = BLFactory.GetInstanceBackGround<SanderModuleItemBL>();
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword = BLFactory.GetInstanceBackGround<TBSanderModuleItemKeywordBL>();

            try
            {
                while (true)
                {
                    List<SanderModuleItemDM> batch = blSanderModuleItem.GetListNeedExtractKeyword(BatchSize);
                    if (batch.Count == 0)
                        break;

                    bool batchSucceeded = await TryProcessBatchWithRetryAsync(
                        batch, blSanderModuleItem, blTBSanderModuleItemKeyword, logDM);

                    if (batchSucceeded)
                    {
                        consecutiveFailedBatchCount = 0;
                    }
                    else
                    {
                        consecutiveFailedBatchCount++;
                        if (consecutiveFailedBatchCount >= MaxConsecutiveFailedBatches)
                        {
                            Method.LogSystem(
                                $"[{LogControllerName}] 連續失敗批次達 {MaxConsecutiveFailedBatches} 次，提前終止工作",
                                ControllerName: LogControllerName);
                            break;
                        }
                    }

                    await Task.Delay(1000); // 每批次處理完後暫停 1 秒，避免對 API 造成過大壓力
                }

                if (logDM != null)
                    logDM.JobStatus = logDM.ErrorCount == 0 ? "Success" : logDM.DataCount > 0 ? "PartialFail" : "Failed";
            }
            catch (Exception ex)
            {
                Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                if (logDM != null)
                {
                    logDM.JobStatus = "Failed";
                    logDM.ErrorMessage = ex.Message;
                }
            }
            finally
            {
                // 工作結束後，將所有標為錯誤（2）的料品重置為待處理（1），供下次執行
                try { blSanderModuleItem.DoResetErrorItems(); }
                catch (Exception resetEx) { Method.LogSystem(resetEx.ToString(), ControllerName: LogControllerName); }
            }
        }

        /// <summary>
        /// 正規化料品資料
        /// </summary>
        /// <param name="source">原始料品清單</param>
        /// <returns>正規化後的料品清單</returns>
        private static List<SanderModuleItemDM> Normalize(List<SanderModuleItemDM> source)
        {
            List<SanderModuleItemDM> result = [];
            foreach (SanderModuleItemDM item in source)
            {
                SanderModuleItemDM normalized = item;
                normalized.Description = SandermoduleItemNormalizer.Normalize(item.Description ?? string.Empty);
                normalized.Description2 = SandermoduleItemNormalizer.Normalize(item.Description2 ?? string.Empty);
                normalized.LongDesc = SandermoduleItemNormalizer.Normalize(item.LongDesc ?? string.Empty);
                normalized.LongDesc2 = SandermoduleItemNormalizer.Normalize(item.LongDesc2 ?? string.Empty);
                result.Add(normalized);
            }
            return result;
        }

        /// <summary>
        /// 處理單一批次（含重試）：API 失敗時重試，成功則依 AI 回傳寫入並僅更新有關鍵字料號的旗標
        /// </summary>
        /// <returns>該批次是否至少有一筆成功寫入；全部失敗（含重試用盡）則 false</returns>
        private async Task<bool> TryProcessBatchWithRetryAsync(
            List<SanderModuleItemDM> batch,
            SanderModuleItemBL blSanderModuleItem,
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword,
            EsScheduleCycleLogDetailDM? logDM)
        {
            List<Guid> batchIds = batch.Select(x => x.Id).ToList();
            int maxAttempts = MaxRetryPerBatch + 1;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    List<TBSanderModuleItemKeywordDM> dataList = await ProcessBatchAsync(batch);
                    SaveBatchExtractKeywordResult(batch, dataList, blTBSanderModuleItemKeyword, logDM);
                    return true;
                }
                catch (Exception ex)
                {
                    bool isLastAttempt = attempt >= maxAttempts;
                    string attemptInfo = $"第 {attempt}/{maxAttempts} 次";
                    Method.LogSystem(
                        $"[{LogControllerName}] 批次處理失敗（{attemptInfo}）\n{ex}",
                        ControllerName: LogControllerName);

                    if (!isLastAttempt)
                    {
                        await Task.Delay(2000);
                        continue;
                    }

                    if (logDM != null)
                        logDM.ErrorCount += batch.Count;

                    try { blSanderModuleItem.GetDAO().UpdateFlagNeedExtractKeyword(batchIds, 2); }
                    catch (Exception markEx) { Method.LogSystem(markEx.ToString(), ControllerName: LogControllerName); }

                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 比對批次輸入與 AI 回傳，僅寫入有關鍵字的料號；漏回傳者維持 flag=1
        /// </summary>
        /// <returns>成功寫入的料號筆數</returns>
        private int SaveBatchExtractKeywordResult(
            List<SanderModuleItemDM> batch,
            List<TBSanderModuleItemKeywordDM> dataList,
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword,
            EsScheduleCycleLogDetailDM? logDM)
        {
            Dictionary<string, List<TBSanderModuleItemKeywordDM>> keywordsByNo = dataList
                .Where(x => !string.IsNullOrWhiteSpace(x.No) && !string.IsNullOrWhiteSpace(x.Keyword))
                .GroupBy(x => x.No!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            List<SanderModuleItemDM> successfulItems = batch
                .Where(item => !string.IsNullOrEmpty(item.No) && keywordsByNo.ContainsKey(item.No))
                .ToList();

            List<string> missingNos = batch
                .Where(item => !string.IsNullOrEmpty(item.No) && !keywordsByNo.ContainsKey(item.No))
                .Select(item => item.No!)
                .ToList();

            if (missingNos.Count > 0)
            {
                Method.LogSystem(
                    $"[{LogControllerName}] AI 漏回傳 {missingNos.Count}/{batch.Count} 筆，料號：{string.Join(", ", missingNos)}",
                    ControllerName: LogControllerName);

                if (logDM != null)
                    logDM.ErrorCount += missingNos.Count;
            }

            if (successfulItems.Count == 0)
                throw new InvalidOperationException($"批次 {batch.Count} 筆皆無有效關鍵字回傳");

            List<string> successfulNos = successfulItems.Select(x => x.No!).ToList();
            List<Guid> successfulIds = successfulItems.Select(x => x.Id).ToList();
            List<TBSanderModuleItemKeywordDM> successfulKeywords = successfulItems
                .SelectMany(item => keywordsByNo[item.No!])
                .ToList();

            blTBSanderModuleItemKeyword.DoSaveExtractKeyword(successfulNos, successfulIds, successfulKeywords);

            if (logDM != null)
                logDM.DataCount += successfulItems.Count;

            return successfulItems.Count;
        }

        /// <summary>
        /// 處理單一批次：正規化 → AI 抽取關鍵字 → 向量化，回傳結果供後續寫入
        /// </summary>
        /// <param name="batch">料品清單</param>
        /// <returns>已完成向量化的關鍵字清單</returns>
        private async Task<List<TBSanderModuleItemKeywordDM>> ProcessBatchAsync(List<SanderModuleItemDM> batch)
        {
            List<SanderModuleItemDM> normalizeList = Normalize(batch);
            List<TBSanderModuleItemKeywordDM> dataList = await _extractKeywordHandler.ExtractKeyword(normalizeList);
            await _manualBatchEmbedding.FillEmbed(dataList);
            return dataList;
        }
    }
}

