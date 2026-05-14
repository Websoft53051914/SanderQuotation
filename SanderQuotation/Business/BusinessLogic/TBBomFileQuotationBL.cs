using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
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
            _unitOfWork.Commit();
        }

        #endregion -- TBBomFileQuotation --
    }

    public partial class TBBomFileQuotationBL
    {
        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁 DM 清單</returns>
        public PageResult<TBBomFileQuotationDM> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            searchVO.StatusEq = (int)StatusEnum.Enabled;

            PageResult<TBBomFileQuotationDTO> pageResult = GetDAO().GetPageList(pageEntity, searchVO);

            List<TBBomFileQuotationDM> results = [];
            foreach (TBBomFileQuotationDTO item in pageResult.Results)
            {
                TBBomFileQuotationDM dm = _mapper.Map<TBBomFileQuotationDM>(item);
                results.Add(dm);
            }

            return new PageResult<TBBomFileQuotationDM>
            {
                CurrentPage = pageResult.CurrentPage,
                DataCount = pageResult.DataCount,
                PageDataSize = pageResult.PageDataSize,
                Results = results,
            };
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoCreate(TBBomFileQuotationDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            TBBomFileQuotationEntity entity = _mapper.Map<TBBomFileQuotationEntity>(dm);
            entity.Status = (int)StatusEnum.Enabled;
            entity.CreatedBy = account;
            entity.UpdatedBy = account;
            entity.CreatedAt = nowTime;
            entity.UpdatedAt = nowTime;

            GetDAO().Insert(entity);
            dm.Id = entity.Id;
        }

        /// <summary>
        /// 更新資料
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoUpdate(TBBomFileQuotationDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;

            TBBomFileQuotationEntity? entity = GetDAO().FindByPk(dm.Id);
            if (entity != null)
            {
                entity.No = dm.No;
                entity.InternalPurchaseOrderDate = dm.InternalPurchaseOrderDate;
                entity.InternalUnitPriceOriginalCurrency = dm.InternalUnitPriceOriginalCurrency;
                entity.InternalUnitPriceTwd = dm.InternalUnitPriceTwd;
                entity.InternalQuantity = dm.InternalQuantity;
                entity.InternalCurrency = dm.InternalCurrency;
                entity.InternalSupplierName = dm.InternalSupplierName;
                entity.ExternalQuotationDate = dm.ExternalQuotationDate;
                entity.ExternalUnitPriceOriginalCurrency = dm.ExternalUnitPriceOriginalCurrency;
                entity.ExternalUnitPriceTwd = dm.ExternalUnitPriceTwd;
                entity.ExternalMoq = dm.ExternalMoq;
                entity.ExternalCurrency = dm.ExternalCurrency;
                entity.ExternalSupplierName = dm.ExternalSupplierName;
                entity.UpdatedBy = account;
                entity.UpdatedAt = DateTime.Now;

                GetDAO().Update(entity);
            }
        }
    }
}
