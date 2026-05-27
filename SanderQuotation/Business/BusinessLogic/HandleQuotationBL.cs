using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using static Const.Enums;

namespace Business.BusinessLogic
{
    /// <summary>
    /// 處理查價相關邏輯
    /// </summary>
    public partial class HandleQuotationBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public HandleQuotationBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
    }

    /// <summary>
    /// 查料
    /// </summary>
    public partial class HandleQuotationBL
    {
        /// <summary>
        /// 批次儲存查料結果，並更新 EsFileTransferUpload.ProcessStatus = PendingPricingSearch，在同一 transaction 中完成
        /// </summary>
        /// <param name="uploadId">EsFileTransferUpload 主鍵</param>
        /// <param name="results">查料結果清單（BomFileContentDM）：No、IsRecommendedNo 與 PendingDecisionLogs 須已填入</param>
        public void DoSavePartSearchResult(Guid uploadId, List<BomFileContentDM> results)
        {
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLEsFileTransferUpload().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            foreach (BomFileContentDM dm in results)
            {
                GetBLTBBomFileQuotation().DoUpsertPartSearchItem(dm);
                GetBLTBBomFileDecisionLog().DoInsertPendingLogs(dm.Id, dm.PendingDecisionLogs);
            }

            GetBLEsFileTransferUpload().DoUpdateProcessStatus(uploadId, (int)EsFileTransferUploadProcessStatusEnum.PendingPricingSearch);

            _unitOfWork.Commit();
            GetBLTBBomFileQuotation().DoSaveChange = true;
            GetBLEsFileTransferUpload().DoSaveChange = true;
            GetBLTBBomFileDecisionLog().DoSaveChange = true;
        }

        /// <summary>
        /// 批次儲存查料＋查價結果（合併流程），並更新 EsFileTransferUpload.ProcessStatus = PricingDone，在同一 transaction 中完成
        /// 先以 DoUpsertPartSearchItem 建立 TBBomFileQuotation 紀錄（含 No），再以 DoUpdatePriceItem 寫入查價欄位
        /// </summary>
        /// <param name="uploadId">EsFileTransferUpload 主鍵</param>
        /// <param name="results">已完成查料與查價的結果清單（BomFileContentDM）</param>
        public void DoSaveFullSearchResult(Guid uploadId, List<BomFileContentDM> results)
        {
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLEsFileTransferUpload().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            foreach (BomFileContentDM dm in results)
            {
                GetBLTBBomFileQuotation().DoUpsertPartSearchItem(dm);
                GetBLTBBomFileQuotation().DoUpdatePriceItem(dm);
                GetBLTBBomFileDecisionLog().DoInsertPendingLogs(dm.Id, dm.PendingDecisionLogs);
            }

            GetBLEsFileTransferUpload().DoUpdateProcessStatus(uploadId, (int)EsFileTransferUploadProcessStatusEnum.PricingDone);

            _unitOfWork.Commit();
            GetBLTBBomFileQuotation().DoSaveChange = true;
            GetBLEsFileTransferUpload().DoSaveChange = true;
            GetBLTBBomFileDecisionLog().DoSaveChange = true;
        }
    }

    /// <summary>
    /// 查價
    /// </summary>
    public partial class HandleQuotationBL
    {
        /// <summary>
        /// 批次儲存查價結果，並更新 EsFileTransferUpload.ProcessStatus = PricingDone，在同一 transaction 中完成
        /// </summary>
        /// <param name="uploadId">EsFileTransferUpload 主鍵</param>
        /// <param name="results">查價結果清單</param>
        public void DoSavePriceSearchResult(Guid uploadId, List<BomFileContentDM> dmList)
        {
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLEsFileTransferUpload().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            foreach (BomFileContentDM dm in dmList)
            {
                GetBLTBBomFileQuotation().DoUpdatePriceItem(dm);
                GetBLTBBomFileDecisionLog().DoInsertPendingLogs(dm.Id, dm.PendingDecisionLogs);
            }

            GetBLEsFileTransferUpload().DoUpdateProcessStatus(
                uploadId,
                (int)EsFileTransferUploadProcessStatusEnum.PricingDone);

            _unitOfWork.Commit();

            GetBLTBBomFileQuotation().DoSaveChange = true;
            GetBLEsFileTransferUpload().DoSaveChange = true;
            GetBLTBBomFileDecisionLog().DoSaveChange = true;
        }
    }

    /// <summary>
    /// 單筆重新查價
    /// </summary>
    public partial class HandleQuotationBL
    {
        /// <summary>
        /// 儲存單筆內部查價結果（更新 TBBomFileQuotation 內部欄位並寫入決策歷程）
        /// </summary>
        /// <param name="dm">已執行內部查價的 BomFileContentDM，PendingDecisionLogs 須已填入</param>
        public void DoSaveSingleInternalQuotationResult(BomFileContentDM dm)
        {
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            GetBLTBBomFileQuotation().DoUpdateInternalPriceItem(dm);
            GetBLTBBomFileDecisionLog().DoInsertPendingLogs(dm.Id, dm.PendingDecisionLogs);

            _unitOfWork.Commit();
            GetBLTBBomFileQuotation().DoSaveChange = true;
            GetBLTBBomFileDecisionLog().DoSaveChange = true;
        }

        /// <summary>
        /// 儲存單筆外部查價結果（更新 TBBomFileQuotation 外部欄位並寫入決策歷程）
        /// </summary>
        /// <param name="dm">已執行外部查價的 BomFileContentDM，PendingDecisionLogs 須已填入</param>
        public void DoSaveSingleExternalQuotationResult(BomFileContentDM dm)
        {
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            GetBLTBBomFileQuotation().DoUpdateExternalPriceItem(dm);
            GetBLTBBomFileDecisionLog().DoInsertPendingLogs(dm.Id, dm.PendingDecisionLogs);

            _unitOfWork.Commit();
            GetBLTBBomFileQuotation().DoSaveChange = true;
            GetBLTBBomFileDecisionLog().DoSaveChange = true;
        }

        /// <summary>
        /// 儲存單筆現貨優惠價查詢結果（分別對 Mouser / DigiKey）
        /// 先刪除該 BomFileContentId 的舊記錄，再插入新結果與決策歷程
        /// </summary>
        /// <param name="bomFileContentId">BOM 料項識別碼</param>
        /// <param name="mouserDm">Mouser 查價結果 DM（查無結果則為 null）</param>
        /// <param name="dkDm">DigiKey 查價結果 DM（查無結果則為 null）</param>
        /// <param name="decisionLogs">合併後的決策歷程清單（含 Stage，由呼叫端填入）</param>
        public void DoSaveSingleInStockPriceResult(
            Guid bomFileContentId,
            TBBomFileQuotationOtherDM? mouserDm,
            TBBomFileQuotationOtherDM? dkDm,
            List<TBBomFileDecisionLogDM> decisionLogs)
        {
            GetBLTBBomFileQuotationOther().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            // 先刪除舊資料
            SearchVO deleteSearchVO = new();
            deleteSearchVO.BomFileContentIdEq = bomFileContentId;
            GetBLTBBomFileQuotationOther().DeleteByFilter(deleteSearchVO);

            // 儲存 Mouser 結果
            if (mouserDm != null)
                GetBLTBBomFileQuotationOther().DoInsertFromApiResult(
                    bomFileContentId,
                    (int)BomFileQuotationOtherSourceTypeEnum.Mouser,
                    mouserDm);

            // 儲存 DigiKey 結果
            if (dkDm != null)
                GetBLTBBomFileQuotationOther().DoInsertFromApiResult(
                    bomFileContentId,
                    (int)BomFileQuotationOtherSourceTypeEnum.DigiKey,
                    dkDm);

            // 寫入決策歷程（含刪除同階段舊歷程）
            GetBLTBBomFileDecisionLog().DoInsertPendingLogs(bomFileContentId, decisionLogs);

            _unitOfWork.Commit();
            GetBLTBBomFileQuotationOther().DoSaveChange = true;
            GetBLTBBomFileDecisionLog().DoSaveChange = true;
        }
    }

    public partial class HandleQuotationBL
    {
        private SanderModuleItemBL? _blSanderModuleItem = null;
        protected SanderModuleItemBL GetBLSanderModuleItem()
        {
            _blSanderModuleItem ??= new SanderModuleItemBL(_unitOfWork, SessionVO ?? new());
            _blSanderModuleItem._Configuration = _Configuration;

            return _blSanderModuleItem;
        }

        private BomFileContentBL? _blBomFileContent = null;
        protected BomFileContentBL GetBLBomFileContent()
        {
            _blBomFileContent ??= new BomFileContentBL(_unitOfWork, SessionVO ?? new());
            _blBomFileContent._Configuration = _Configuration;

            return _blBomFileContent;
        }

        private TBSanderModuleItemKeywordBL? _blTBSanderModuleItemKeyword = null;
        protected TBSanderModuleItemKeywordBL GetBLTBSanderModuleItemKeyword()
        {
            _blTBSanderModuleItemKeyword ??= new TBSanderModuleItemKeywordBL(_unitOfWork, SessionVO ?? new());
            _blTBSanderModuleItemKeyword._Configuration = _Configuration;

            return _blTBSanderModuleItemKeyword;
        }

        private TBBomFileQuotationBL? _blTBBomFileQuotation = null;
        protected TBBomFileQuotationBL GetBLTBBomFileQuotation()
        {
            _blTBBomFileQuotation ??= new TBBomFileQuotationBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileQuotation._Configuration = _Configuration;

            return _blTBBomFileQuotation;
        }

        private EsFileTransferUploadBL? _blEsFileTransferUpload = null;
        protected EsFileTransferUploadBL GetBLEsFileTransferUpload()
        {
            _blEsFileTransferUpload ??= new EsFileTransferUploadBL(_unitOfWork, SessionVO ?? new());
            _blEsFileTransferUpload._Configuration = _Configuration;

            return _blEsFileTransferUpload;
        }

        private TBBomFileDecisionLogBL? _blTBBomFileDecisionLog = null;
        protected TBBomFileDecisionLogBL GetBLTBBomFileDecisionLog()
        {
            _blTBBomFileDecisionLog ??= new TBBomFileDecisionLogBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileDecisionLog._Configuration = _Configuration;

            return _blTBBomFileDecisionLog;
        }

        private TBBomFileQuotationOtherBL? _blTBBomFileQuotationOther = null;
        protected TBBomFileQuotationOtherBL GetBLTBBomFileQuotationOther()
        {
            _blTBBomFileQuotationOther ??= new TBBomFileQuotationOtherBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileQuotationOther._Configuration = _Configuration;

            return _blTBBomFileQuotationOther;
        }
    }
}
