using backend.Common;
using backend.Models;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using static Const.Enums;

namespace backend.EIPSource
{
    /// <summary>
    /// 查價排程工作
    /// </summary>
    public partial class PriceSearchJob
    {
        /// <summary>
        /// Log 中的 Controller 名稱，方便識別是哪個工作產生的 Log
        /// </summary>
        private const string LogControllerName = nameof(PriceSearchJob);

        private readonly QuotationHandler _quotationHandler;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="quotationHandler">查價處理器</param>
        public PriceSearchJob(QuotationHandler quotationHandler)
        {
            _quotationHandler = quotationHandler;
        }

        /// <summary>
        /// 執行查價工作
        /// 取 ProcessStatus = PartSearchDone(4) 的上傳檔案，對每筆 BomFileContent 執行內部與外部查價，
        /// 結果寫入 TBBomFileQuotation，全部處理完畢後更新 ProcessStatus = PricingDone(5)
        /// </summary>
        /// <param name="logDM">排程執行紀錄；傳入 null 時略過紀錄更新</param>
        public async Task ExecuteAsync(EsScheduleCycleLogDetailDM? logDM = null)
        {
            try
            {
                await _quotationHandler.AuthorizeExternalAsync();

                EsFileTransferUploadBL blEsFileTransferUpload = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();

                SearchVO uploadSearchVO = new();
                uploadSearchVO.ProcessStatusEq = (int)EsFileTransferUploadProcessStatusEnum.PartSearchDone;
                // 取得可處理的上傳檔案清單
                List<EsFileTransferUploadDM> uploads = blEsFileTransferUpload.GetListEnabled(uploadSearchVO).ToList();

                foreach (EsFileTransferUploadDM upload in uploads)
                {
                    await ProcessUploadAsync(upload, logDM);
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
        /// 處理單一上傳檔案的查價作業
        /// </summary>
        /// <param name="upload">上傳檔案 DM</param>
        /// <param name="logDM">排程執行紀錄；傳入 null 時略過紀錄更新</param>
        private async Task ProcessUploadAsync(EsFileTransferUploadDM upload, EsScheduleCycleLogDetailDM? logDM = null)
        {
            try
            {
                BomFileContentBL blBomFileContent = BLFactory.GetInstanceBackGround<BomFileContentBL>();
                HandleQuotationBL blHandleQuotation = BLFactory.GetInstanceBackGround<HandleQuotationBL>();

                SearchVO contentSearchVO = new();
                contentSearchVO.UploadIdEq = upload.UploadId;
                List<BomFileContentDM> contents = blBomFileContent.GetListWithQuotationByFilter(contentSearchVO);
                List<BomFileContentDM> results = new();

                for (int i = 0; i < contents.Count; i++)
                {
                    BomFileContentDM content = contents[i];

                    try
                    {
                        _quotationHandler.RunExternal = false;
                        //if (i == 0 || i == 1)
                        //{
                        //    _quotationHandler.RunExternal = true;
                        //}

                        BomFileContentDM result = await _quotationHandler.RunAsync(content, upload);
                        results.Add(result);
                        if (logDM != null)
                            logDM.DataCount++;
                    }
                    catch (Exception ex)
                    {
                        Method.LogSystem($"[查價錯誤] UploadId={upload.UploadId} ContentId={content.Id}\n{ex}", ControllerName: LogControllerName);
                        if (logDM != null)
                            logDM.ErrorCount++;
                    }
                }

                blHandleQuotation.DoSavePriceSearchResult(upload.Id, results);
            }
            catch (Exception ex)
            {
                Method.LogSystem($"[查價流程錯誤] UploadId={upload.UploadId}\n{ex}", ControllerName: LogControllerName);
                if (logDM != null)
                    logDM.ErrorCount++;
            }
        }
    }
}

