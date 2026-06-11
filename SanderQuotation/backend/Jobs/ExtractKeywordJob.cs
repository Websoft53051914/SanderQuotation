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
        /// 執行關鍵字抽取工作
        /// 取 FlagNeedExtractKeyword = 1 的料品，每批 30 筆，處理後更新旗標為 0；
        /// 批次失敗時將該批次旗標標為 2（錯誤），累計錯誤批次達 3 次則提前終止。
        /// 工作結束後無論成功/失敗，將所有 2（錯誤）重置為 1（待處理）供下次執行。
        /// </summary>
        /// <param name="logDM">排程執行紀錄，供呼叫端彙總結果；傳入 null 時略過紀錄更新</param>
        public async Task ExecuteAsync(EsScheduleCycleLogDetailDM logDM = null)
        {
            const int MaxErrorBatches = 3;
            int errorBatchCount = 0;

            SanderModuleItemBL blSanderModuleItem = BLFactory.GetInstanceBackGround<SanderModuleItemBL>();
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword = BLFactory.GetInstanceBackGround<TBSanderModuleItemKeywordBL>();

            try
            {
                while (true)
                {
                    List<SanderModuleItemDM> batch = blSanderModuleItem.GetListNeedExtractKeyword(BatchSize);
                    if (batch.Count == 0)
                        break;

                    List<Guid> batchIds = batch.Select(x => x.Id).ToList();
                    List<string> batchNos = batch
                        .Where(x => !string.IsNullOrEmpty(x.No))
                        .Select(x => x.No!)
                        .ToList();

                    try
                    {
                        List<TBSanderModuleItemKeywordDM> dataList = await ProcessBatchAsync(batch);
                        blTBSanderModuleItemKeyword.DoSaveExtractKeyword(batchNos, batchIds, dataList);
                        if (logDM != null)
                            logDM.DataCount += batch.Count;
                    }
                    catch (Exception ex)
                    {
                        Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                        if (logDM != null)
                            logDM.ErrorCount += batch.Count;

                        // 將此批次標為錯誤狀態（2），避免下次循環重複取到
                        try { blSanderModuleItem.GetDAO().UpdateFlagNeedExtractKeyword(batchIds, 2); }
                        catch (Exception markEx) { Method.LogSystem(markEx.ToString(), ControllerName: LogControllerName); }

                        errorBatchCount++;
                        if (errorBatchCount >= MaxErrorBatches)
                        {
                            Method.LogSystem($"[{LogControllerName}] 錯誤批次達 {MaxErrorBatches} 次，提前終止工作", ControllerName: LogControllerName);
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

