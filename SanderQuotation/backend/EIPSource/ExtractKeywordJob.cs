using backend.AI;
using backend.Common;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;

namespace backend.EIPSource
{
    /// <summary>
    /// 內部料品表 AI 關鍵字抽取排程工作
    /// </summary>
    public class ExtractKeywordJob
    {
        /// <summary>
        /// 批次大小<para/>
        /// 若設定為 100 筆，有機會因為單次處理資料過多而導致 API 請求失敗
        /// </summary>
        private const int BatchSize = 50;
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
        /// 取 FlagNeedExtractKeyword = true 的料品，每批 50 筆，處理後更新旗標為 false
        /// </summary>
        public async Task ExecuteAsync()
        {
            try
            {
                SanderModuleItemBL blSanderModuleItem = BLFactory.GetInstanceBackGround<SanderModuleItemBL>();
                TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword = BLFactory.GetInstanceBackGround<TBSanderModuleItemKeywordBL>();

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
                    }
                    catch (Exception ex)
                    {
                        Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                    }

                    await Task.Delay(1000); // 每批次處理完後暫停 1 秒，避免對 API 造成過大壓力
                }
            }
            catch (Exception ex)
            {
                Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
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

