using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Business.BusinessLogic
{
    /// <summary>
    /// BOM 查價(料)結果
    /// </summary>
    public partial class TBBomFileQuotationBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public TBBomFileQuotationBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<TBBomFileQuotationEntity, TBBomFileQuotationDM>().ReverseMap();
                c.CreateMap<TBBomFileQuotationDTO, TBBomFileQuotationDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public TBBomFileQuotationBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class TBBomFileQuotationBL
    {
        #region -- TBBomFileQuotation --

        private ITBBomFileQuotationDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ITBBomFileQuotationDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ITBBomFileQuotationDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileQuotationDM> GetListByFilter(SearchVO searchVO)
        {
            List<TBBomFileQuotationDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<TBBomFileQuotationDM> result = [];
            foreach (TBBomFileQuotationDTO item in dtoList)
            {
                TBBomFileQuotationDM dm = _mapper.Map<TBBomFileQuotationDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得啟用的資料清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileQuotationDM> GetListEnabled(SearchVO searchVO)
        {
            searchVO.StatusEq = (int)Enums.StatusEnum.Enabled;

            return GetListByFilter(searchVO);
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public TBBomFileQuotationDM? GetOneInfo(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            searchVO.IsLimit1 = true;

            return GetListByFilter(searchVO).FirstOrDefault();
        }

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        public void DeleteByFilter(SearchVO searchVO)
        {
            string account = SessionVO?.Account ?? string.Empty;
            GetDAO().DeleteByFilter(searchVO, account);
            if (DoSaveChange)
                _unitOfWork.Commit();
        }

        #endregion -- TBBomFileQuotation --

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public TBBomFileQuotationDM? GetOneByBomFileContentId(Guid pBomFileContentId)
        {
            SearchVO searchVO = new();
            searchVO.BomFileContentIdEq = pBomFileContentId;
            searchVO.IsLimit1 = true;

            return GetListEnabled(searchVO).FirstOrDefault();
        }
    }

    public partial class TBBomFileQuotationBL
    {
        /// <summary>
        /// 更新 TBBomFileQuotation 的採購型號與是否為建議料號
        /// </summary>
        /// <param name="bomFileContentId">BomFileContent.Id</param>
        /// <param name="no">採購型號</param>
        /// <param name="isRecommendedNo">是否為建議料號</param>
        public void DoUpdateNo(Guid bomFileContentId, string? no, bool isRecommendedNo)
        {
            string account = SessionVO?.Account ?? string.Empty;

            TBBomFileQuotationDM? dm = GetOneByBomFileContentId(bomFileContentId);
            if (dm == null)
                return;

            TBBomFileQuotationEntity? entity = GetDAO().FindByPk(dm.Id);
            if (entity == null)
                return;

            entity.No = no;
            entity.IsRecommendedNo = isRecommendedNo;
            entity.UpdatedBy = account;
            entity.UpdatedAt = DateTime.Now;
            GetDAO().Update(entity);
            if (DoSaveChange)
                _unitOfWork.Commit();
        }

        /// <summary>
        /// 依 BomFileContentDM 的查料結果，新增或更新 TBBomFileQuotation 之查料欄位
        /// （No、IsRecommendedNo、MatchCategory、MatchField）
        /// </summary>
        /// <param name="dm">BOM 料項 DM，dm.Id 為 BomFileContentId</param>
        public void DoUpsertPartSearchItem(BomFileContentDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            TBBomFileQuotationDM? existing = GetOneByBomFileContentId(dm.Id);
            if (existing != null)
            {
                TBBomFileQuotationEntity? entity = GetDAO().FindByPk(existing.Id);
                if (entity != null)
                {
                    entity.No = dm.No;
                    entity.IsRecommendedNo = dm.IsRecommendedNo;
                    entity.MatchCategory = dm.MatchCategory;
                    entity.MatchField = dm.MatchField;
                    entity.Status = (int)StatusEnum.Enabled;
                    entity.UpdatedBy = account;
                    entity.UpdatedAt = nowTime;
                    GetDAO().Update(entity);
                }
            }
            else
            {
                TBBomFileQuotationEntity entity = new();
                entity.No = dm.No;
                entity.IsRecommendedNo = dm.IsRecommendedNo;
                entity.MatchCategory = dm.MatchCategory;
                entity.MatchField = dm.MatchField;
                entity.BomFileContentId = dm.Id;
                entity.Status = (int)StatusEnum.Enabled;
                entity.CreatedBy = account;
                entity.UpdatedBy = account;
                entity.CreatedAt = nowTime;
                entity.UpdatedAt = nowTime;
                GetDAO().Insert(entity);
            }

            if (DoSaveChange)
                _unitOfWork.Commit();
        }

        /// <summary>
        /// 依 BomFileContentDM 更新 TBBomFileQuotation 內部查價欄位
        /// </summary>
        /// <param name="dm">BOM 料項 DM，dm.Id 為 BomFileContentId</param>
        public void DoUpdateInternalPriceItem(BomFileContentDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            TBBomFileQuotationDM? existing = GetOneByBomFileContentId(dm.Id);
            if (existing == null)
                return;

            TBBomFileQuotationEntity? entity = GetDAO().FindByPk(existing.Id);
            if (entity == null)
                return;

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
            entity.InternalSupplierCode = dm.InternalSupplierCode;
            entity.InternalItemDescription2 = dm.InternalItemDescription2;
            entity.IsFilterByCustomerApprovedPart = dm.IsFilterByCustomerApprovedPart;
            entity.CustomerApprovedPartCsv = dm.CustomerApprovedPartCsv;
            entity.InternalQuotationDate = nowTime;
            entity.UpdatedBy = account;
            entity.UpdatedAt = nowTime;
            GetDAO().Update(entity);

            if (DoSaveChange)
                _unitOfWork.Commit();
        }

        /// <summary>
        /// 依 BomFileContentDM 更新 TBBomFileQuotation 外部查價欄位
        /// </summary>
        /// <param name="dm">BOM 料項 DM，dm.Id 為 BomFileContentId</param>
        public void DoUpdateExternalPriceItem(BomFileContentDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            TBBomFileQuotationDM? existing = GetOneByBomFileContentId(dm.Id);
            if (existing == null)
                return;

            TBBomFileQuotationEntity? entity = GetDAO().FindByPk(existing.Id);
            if (entity == null)
                return;

            entity.ExternalQuotationDate = dm.ExternalQuotationDate;
            entity.ExternalUnitPriceOriginalCurrency = dm.ExternalUnitPriceOriginalCurrency;
            entity.ExternalUnitPriceTwd = dm.ExternalUnitPriceTwd;
            entity.ExternalMoq = dm.ExternalMoq;
            entity.ExternalSupplierName = dm.ExternalSupplierName;
            entity.ExternalCurrency = dm.ExternalCurrency;
            entity.ExternalStock = dm.ExternalStock;
            entity.ExternalScenario = dm.ExternalScenario;
            entity.UpdatedBy = account;
            entity.UpdatedAt = nowTime;
            GetDAO().Update(entity);

            if (DoSaveChange)
                _unitOfWork.Commit();
        }

        /// <summary>
        /// 取得效期內其他 BomFileContent 的最新內部查價結果（排除指定 BomFileContentId）
        /// </summary>
        /// <param name="itemNo">採購型號</param>
        /// <param name="expirationDays">效期天數</param>
        /// <param name="excludeBomFileContentId">排除的 BomFileContentId（目前料項本身）</param>
        /// <returns>最近一筆有效的查價紀錄，找不到則回傳 null</returns>
        public TBBomFileQuotationDM? GetOneForExpirationCache(string itemNo, int expirationDays, Guid excludeBomFileContentId)
        {
            SearchVO searchVO = new();
            searchVO.SanderModuleItemNoEq = itemNo;
            searchVO.InternalQuotationDateGte = DateTime.Now.AddDays(-expirationDays);
            searchVO.BomFileContentIdNeq = excludeBomFileContentId;
            searchVO.IsLimit1 = true;
            searchVO.OrderByColumnList = [nameof(SearchVO.InternalQuotationDateOdr)];
            searchVO.InternalQuotationDateOdr = "DESC";
            return GetListEnabled(searchVO).FirstOrDefault();
        }
    }
}
