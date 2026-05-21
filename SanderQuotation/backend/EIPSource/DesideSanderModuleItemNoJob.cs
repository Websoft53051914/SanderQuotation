using backend.Common;
using backend.Models;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using static Const.Enums;

namespace backend.EIPSource
{
    /// <summary>
    /// 決定採購型號排程工作
    /// </summary>
    public partial class DesideSanderModuleItemNoJob
    {
        /// <summary>
        /// Log 中的 Controller 名稱，方便識別是哪個工作產生的 Log
        /// </summary>
        private const string LogControllerName = nameof(DesideSanderModuleItemNoJob);

        private readonly QuotationHandler _quotationHandler;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="quotationHandler">查價/查料處理器</param>
        public DesideSanderModuleItemNoJob(QuotationHandler quotationHandler)
        {
            _quotationHandler = quotationHandler;
        }

        /// <summary>
        /// 執行查料工作
        /// 取 ProcessStatus = Transferred(2) 且為 BOM 檔案規則的上傳檔案，對每筆 BomFileContent 執行查料，
        /// 結果寫入 TBBomFileQuotation，全部處理完畢後更新 ProcessStatus = PendingPartSearch(3)
        /// </summary>
        /// <param name="logDM">排程執行紀錄，供呼叫端彙總結果；傳入 null 時略過紀錄更新</param>
        public async Task ExecuteAsync(EsScheduleCycleLogDetailDM? logDM = null)
        {
            try
            {
                EsFileTransferUploadBL blEsFileTransferUpload = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();

                // 取得需比對廠牌之料品類別清單（此次執行共用，避免每筆重複查詢）
                HashSet<string> brandComparisonCategorySet = _quotationHandler.GetBrandComparisonCategorySet();

                // 取得 ProcessStatus = PendingPartSearch 的上傳檔案
                SearchVO uploadSearchVO = new();
                uploadSearchVO.ProcessStatusEq = (int)EsFileTransferUploadProcessStatusEnum.PendingPartSearch;

                List<EsFileTransferUploadDM> uploads = blEsFileTransferUpload.GetListEnabled(uploadSearchVO)
                   .ToList();

                foreach (EsFileTransferUploadDM upload in uploads)
                {
                    await ProcessUploadAsync(upload, brandComparisonCategorySet, logDM);
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
        }

        /// <summary>
        /// 處理單一上傳檔案的查料作業
        /// </summary>
        /// <param name="upload">上傳檔案 DM</param>
        /// <param name="brandComparisonCategorySet">需比對廠牌之料品類別集合（此次執行共用）</param>
        /// <param name="logDM">排程執行紀錄；傳入 null 時略過紀錄更新</param>
        private async Task ProcessUploadAsync(EsFileTransferUploadDM upload, HashSet<string> brandComparisonCategorySet, EsScheduleCycleLogDetailDM? logDM = null)
        {
            try
            {
                BomFileContentBL blBomFileContent = BLFactory.GetInstanceBackGround<BomFileContentBL>();
                HandleQuotationBL blHandleQuotation = BLFactory.GetInstanceBackGround<HandleQuotationBL>();

                List<BomFileContentDM> contents = blBomFileContent.GetListByUploadId(upload.UploadId);
                List<BomFileContentDM> results = new();

                foreach (BomFileContentDM content in contents)
                {
                    try
                    {
                        await _quotationHandler.RunPartSearchAsync(content, brandComparisonCategorySet);
                        results.Add(content);
                        if (logDM != null)
                            logDM.DataCount++;
                    }
                    catch (Exception ex)
                    {
                        Method.LogSystem($"[查料錯誤] UploadId={upload.UploadId} ContentId={content.Id}\n{ex}", ControllerName: LogControllerName);
                        if (logDM != null)
                            logDM.ErrorCount++;
                    }
                }

                // 全部 BomFileContent 處理完畢，批次儲存查料結果並更新狀態為 PartSearchDone（同一 transaction）
                blHandleQuotation.DoSavePartSearchResult(upload.Id, results);
            }
            catch (Exception ex)
            {
                Method.LogSystem($"[查料流程錯誤] UploadId={upload.UploadId}\n{ex}", ControllerName: LogControllerName);
                if (logDM != null)
                    logDM.ErrorCount++;
            }
        }
    }
}
