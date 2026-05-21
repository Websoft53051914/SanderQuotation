using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Business.BusinessLogic
{
    /// <summary>
    /// 處理查價相關邏輯
    /// </summary>
    public partial class HandleQuotationBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public HandleQuotationBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
            });
            _mapper = cfg.CreateMapper();
        }
    }

    /// <summary>
    /// 內部料品表 AI 關鍵字抽取
    /// </summary>
    public partial class HandleQuotationBL
    {
        /// <summary>
        /// 儲存料品關鍵字抽取結果：先刪除舊資料、新增關鍵字、更新 FlagNeedExtractKeyword 旗標，三步共用同一連線
        /// </summary>
        /// <param name="noList">料號清單（用於刪除舊關鍵字與更新旗標）</param>
        /// <param name="ids">資料 Id 清單（用於更新 FlagNeedExtractKeyword）</param>
        /// <param name="dmList">新關鍵字 DM 清單</param>
        public void DoSaveExtractKeyword(List<string> noList, List<Guid> ids, List<TBSanderModuleItemKeywordDM> dmList)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;
            GetBLTBSanderModuleItemKeyword().DoSaveChange = false;

            // 刪除舊關鍵字
            if (noList.Count > 0)
            {
                SearchVO deleteSearchVO = new();
                deleteSearchVO.SanderModuleItemNoIn = noList;

                GetBLTBSanderModuleItemKeyword().DeleteByFilter(deleteSearchVO);
            }

            // 新增關鍵字
            foreach (TBSanderModuleItemKeywordDM dm in dmList)
            {
                GetBLTBSanderModuleItemKeyword().DoInsert(dm);
            }

            // 更新 FlagNeedExtractKeyword
            if (ids.Count > 0)
            {
                GetBLSanderModuleItem().GetDAO().UpdateFlagNeedExtractKeyword(ids, false);
            }

            _unitOfWork.Commit();
            GetBLTBSanderModuleItemKeyword().DoSaveChange = true;
        }
    }

    /// <summary>
    /// 查料
    /// </summary>
    public partial class HandleQuotationBL
    {
        /// <summary>
        /// 批次儲存查料結果，並更新 EsFileTransferUpload.ProcessStatus = PartSearchDone，在同一 transaction 中完成
        /// </summary>
        /// <param name="uploadId">EsFileTransferUpload 主鍵</param>
        /// <param name="results">查料結果清單（BomFileContentDM）：No、IsRecommendedNo 與 PendingDecisionLogs 須已填入</param>
        public void DoSavePartSearchResult(Guid uploadId, List<BomFileContentDM> results)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLEsFileTransferUpload().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            foreach (BomFileContentDM dm in results)
            {
                SearchVO searchVO = new();
                searchVO.BomFileContentIdEq = dm.Id;
                searchVO.IsLimit1 = true;

                TBBomFileQuotationDM? existing = GetBLTBBomFileQuotation().GetListByFilter(searchVO).FirstOrDefault();
                if (existing != null)
                {
                    TBBomFileQuotationEntity? entity = GetBLTBBomFileQuotation().GetDAO().FindByPk(existing.Id);
                    if (entity != null)
                    {
                        entity.No = dm.No;
                        entity.IsRecommendedNo = dm.IsRecommendedNo;
                        entity.Status = (int)StatusEnum.Enabled;
                        entity.UpdatedBy = account;
                        entity.UpdatedAt = nowTime;
                        GetBLTBBomFileQuotation().GetDAO().Update(entity);
                    }
                }
                else
                {
                    TBBomFileQuotationEntity entity = new();
                    entity.No = dm.No;
                    entity.IsRecommendedNo = dm.IsRecommendedNo;
                    entity.BomFileContentId = dm.Id;
                    entity.Status = (int)StatusEnum.Enabled;
                    entity.CreatedBy = account;
                    entity.UpdatedBy = account;
                    entity.CreatedAt = nowTime;
                    entity.UpdatedAt = nowTime;
                    GetBLTBBomFileQuotation().GetDAO().Insert(entity);
                }

                // 寫入暫存決策歷程
                foreach (PendingDecisionLogVO log in dm.PendingDecisionLogs)
                {
                    TBBomFileDecisionLogDM logDM = new();
                    logDM.BomFileContentId = dm.Id;
                    logDM.Stage = log.Stage;
                    logDM.Step = log.Step;
                    logDM.Message = log.Message;
                    GetBLTBBomFileDecisionLog().DoInsert(logDM);
                }
            }

            GetBLEsFileTransferUpload().DoUpdateProcessStatus(uploadId, (int)EsFileTransferUploadProcessStatusEnum.PartSearchDone);

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
        public void DoSavePriceSearchResult(Guid uploadId, List<BomFileContentDM> results)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLEsFileTransferUpload().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            foreach (BomFileContentDM dm in results)
            {
                SearchVO searchVO = new();
                searchVO.BomFileContentIdEq = dm.Id;
                searchVO.IsLimit1 = true;

                TBBomFileQuotationDM? existing = GetBLTBBomFileQuotation().GetListByFilter(searchVO).FirstOrDefault();
                if (existing == null)
                    continue;

                TBBomFileQuotationEntity? entity = GetBLTBBomFileQuotation().GetDAO().FindByPk(existing.Id);
                if (entity == null)
                    continue;

                entity.InternalPurchaseOrderDate = dm.InternalPurchaseOrderDate;
                entity.InternalUnitPriceOriginalCurrency = dm.InternalUnitPriceOriginalCurrency;
                entity.InternalUnitPriceTwd = dm.InternalUnitPriceTwd;
                entity.InternalQuantity = dm.InternalQuantity;
                entity.InternalLowMinPrice = dm.InternalLowMinPrice;
                entity.InternalLowMaxPrice = dm.InternalLowMaxPrice;
                entity.InternalHighMinPrice = dm.InternalHighMinPrice;
                entity.InternalHighMaxPrice = dm.InternalHighMaxPrice;
                entity.InternalSupplierName = dm.InternalSupplierName;
                entity.InternalCurrency = dm.InternalCurrency;
                entity.IsFilterByCustomerApprovedPart = dm.IsFilterByCustomerApprovedPart;
                entity.CustomerApprovedPartCsv = dm.CustomerApprovedPartCsv;
                entity.ExternalQuotationDate = dm.ExternalQuotationDate;
                entity.ExternalUnitPriceOriginalCurrency = dm.ExternalUnitPriceOriginalCurrency;
                entity.ExternalUnitPriceTwd = dm.ExternalUnitPriceTwd;
                entity.ExternalMoq = dm.ExternalMoq;
                entity.ExternalSupplierName = dm.ExternalSupplierName;
                entity.ExternalCurrency = dm.ExternalCurrency;
                entity.UpdatedBy = account;
                entity.UpdatedAt = nowTime;

                GetBLTBBomFileQuotation().GetDAO().Update(entity);

                // 寫入暫存決策歷程
                foreach (PendingDecisionLogVO log in dm.PendingDecisionLogs)
                {
                    TBBomFileDecisionLogDM logDM = new();
                    logDM.BomFileContentId = dm.Id;
                    logDM.Stage = log.Stage;
                    logDM.Step = log.Step;
                    logDM.Message = log.Message;
                    GetBLTBBomFileDecisionLog().DoInsert(logDM);
                }
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
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            SearchVO searchVO = new();
            searchVO.BomFileContentIdEq = dm.Id;
            searchVO.IsLimit1 = true;

            TBBomFileQuotationDM? existing = GetBLTBBomFileQuotation().GetListByFilter(searchVO).FirstOrDefault();
            if (existing != null)
            {
                TBBomFileQuotationEntity? entity = GetBLTBBomFileQuotation().GetDAO().FindByPk(existing.Id);
                if (entity != null)
                {
                    entity.InternalPurchaseOrderDate = dm.InternalPurchaseOrderDate;
                    entity.InternalUnitPriceOriginalCurrency = dm.InternalUnitPriceOriginalCurrency;
                    entity.InternalUnitPriceTwd = dm.InternalUnitPriceTwd;
                    entity.InternalQuantity = dm.InternalQuantity;
                    entity.InternalLowMinPrice = dm.InternalLowMinPrice;
                    entity.InternalLowMaxPrice = dm.InternalLowMaxPrice;
                    entity.InternalHighMinPrice = dm.InternalHighMinPrice;
                    entity.InternalHighMaxPrice = dm.InternalHighMaxPrice;
                    entity.InternalSupplierName = dm.InternalSupplierName;
                    entity.InternalCurrency = dm.InternalCurrency;
                    entity.IsFilterByCustomerApprovedPart = dm.IsFilterByCustomerApprovedPart;
                    entity.CustomerApprovedPartCsv = dm.CustomerApprovedPartCsv;
                    entity.UpdatedBy = account;
                    entity.UpdatedAt = nowTime;
                    GetBLTBBomFileQuotation().GetDAO().Update(entity);
                }
            }

            foreach (PendingDecisionLogVO log in dm.PendingDecisionLogs)
            {
                TBBomFileDecisionLogDM logDM = new();
                logDM.BomFileContentId = dm.Id;
                logDM.Stage = log.Stage;
                logDM.Step = log.Step;
                logDM.Message = log.Message;
                GetBLTBBomFileDecisionLog().DoInsert(logDM);
            }

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
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLTBBomFileDecisionLog().DoSaveChange = false;

            SearchVO searchVO = new();
            searchVO.BomFileContentIdEq = dm.Id;
            searchVO.IsLimit1 = true;

            TBBomFileQuotationDM? existing = GetBLTBBomFileQuotation().GetListByFilter(searchVO).FirstOrDefault();
            if (existing != null)
            {
                TBBomFileQuotationEntity? entity = GetBLTBBomFileQuotation().GetDAO().FindByPk(existing.Id);
                if (entity != null)
                {
                    entity.ExternalQuotationDate = dm.ExternalQuotationDate;
                    entity.ExternalUnitPriceOriginalCurrency = dm.ExternalUnitPriceOriginalCurrency;
                    entity.ExternalUnitPriceTwd = dm.ExternalUnitPriceTwd;
                    entity.ExternalMoq = dm.ExternalMoq;
                    entity.ExternalSupplierName = dm.ExternalSupplierName;
                    entity.ExternalCurrency = dm.ExternalCurrency;
                    entity.UpdatedBy = account;
                    entity.UpdatedAt = nowTime;
                    GetBLTBBomFileQuotation().GetDAO().Update(entity);
                }
            }

            foreach (PendingDecisionLogVO log in dm.PendingDecisionLogs)
            {
                TBBomFileDecisionLogDM logDM = new();
                logDM.BomFileContentId = dm.Id;
                logDM.Stage = log.Stage;
                logDM.Step = log.Step;
                logDM.Message = log.Message;
                GetBLTBBomFileDecisionLog().DoInsert(logDM);
            }

            _unitOfWork.Commit();
            GetBLTBBomFileQuotation().DoSaveChange = true;
            GetBLTBBomFileDecisionLog().DoSaveChange = true;
        }
    }
}
