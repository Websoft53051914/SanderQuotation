using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Business.BusinessLogic
{
    /// <summary>
    /// BOM 外部查價歷史
    /// </summary>
    public partial class TBBomFileQuotationExternalHistoryBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public TBBomFileQuotationExternalHistoryBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<TBBomFileQuotationExternalHistoryEntity, TBBomFileQuotationExternalHistoryDM>().ReverseMap();
                c.CreateMap<TBBomFileQuotationExternalHistoryDTO, TBBomFileQuotationExternalHistoryDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public TBBomFileQuotationExternalHistoryBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class TBBomFileQuotationExternalHistoryBL
    {
        #region -- TBBomFileQuotationExternalHistory --

        private ITBBomFileQuotationExternalHistoryDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ITBBomFileQuotationExternalHistoryDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ITBBomFileQuotationExternalHistoryDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileQuotationExternalHistoryDM> GetListByFilter(SearchVO searchVO)
        {
            List<TBBomFileQuotationExternalHistoryDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<TBBomFileQuotationExternalHistoryDM> result = [];
            foreach (TBBomFileQuotationExternalHistoryDTO item in dtoList)
            {
                TBBomFileQuotationExternalHistoryDM dm = _mapper.Map<TBBomFileQuotationExternalHistoryDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得啟用的資料清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileQuotationExternalHistoryDM> GetListEnabled(SearchVO searchVO)
        {
            searchVO.StatusEq = (int)Enums.StatusEnum.Enabled;

            return GetListByFilter(searchVO);
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public TBBomFileQuotationExternalHistoryDM? GetOneInfo(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            searchVO.IsLimit1 = true;

            return GetListByFilter(searchVO).FirstOrDefault();
        }

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="isLogicalDelete">true 為邏輯刪除 (UPDATE status)；false 為物理刪除 (DELETE)</param>
        public void DeleteByFilter(SearchVO searchVO, bool isLogicalDelete = true)
        {
            string account = SessionVO?.Account ?? string.Empty;
            GetDAO().DeleteByFilter(searchVO, account, isLogicalDelete);
            if (DoSaveChange)
            {
                _unitOfWork.Commit();
            }
        }

        #endregion -- TBBomFileQuotationExternalHistory --

        /// <summary>
        /// 新增一筆外部查價歷史
        /// </summary>
        /// <param name="dm">DM 物件，查價相關欄位須已填妥</param>
        public void DoInsert(TBBomFileQuotationExternalHistoryDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            dm.Status = (int)Enums.StatusEnum.Enabled;
            dm.CreatedAt = nowTime;
            dm.CreatedBy = account;
            dm.UpdatedAt = nowTime;
            dm.UpdatedBy = account;

            TBBomFileQuotationExternalHistoryEntity entity = _mapper.Map<TBBomFileQuotationExternalHistoryEntity>(dm);
            GetDAO().InsertAction(entity);
            if (DoSaveChange)
            {
                _unitOfWork.Commit();
            }
        }

        /// <summary>
        /// 批次新增外部查價歷史，統一 Commit 一次
        /// </summary>
        /// <param name="dmList">DM 清單</param>
        public void BatchInsert(List<TBBomFileQuotationExternalHistoryDM> dmList)
        {
            DoSaveChange = false;

            foreach (TBBomFileQuotationExternalHistoryDM dm in dmList)
            {
                DoInsert(dm);
            }

            DoSaveChange = true;
            _unitOfWork.Commit();
        }

        /// <summary>
        /// 依廠商型號查詢效期內所有啟用的外部查價歷史清單
        /// </summary>
        /// <param name="mpn">廠商型號</param>
        /// <param name="expirationDays">效期天數</param>
        /// <returns>效期內的外部查價歷史清單</returns>
        public List<TBBomFileQuotationExternalHistoryDM> GetListForExpirationCache(string mpn, int expirationDays)
        {
            SearchVO searchVO = new();
            searchVO.ManufacturerPartNumberEq = mpn;
            searchVO.QuotationDateGte = DateTime.Now.Date.AddDays(-expirationDays);
            return GetListEnabled(searchVO);
        }
    }

    public partial class TBBomFileQuotationExternalHistoryBL
    {
        /// <summary>
        /// 物理刪除查價日期早於指定天數的歷史紀錄
        /// </summary>
        /// <param name="expirationDays">效期天數，超過此天數的紀錄將被刪除</param>
        public void DeleteOldRecords(int expirationDays)
        {
            SearchVO searchVO = new();
            searchVO.QuotationDateLt = DateTime.Now.Date.AddDays(-expirationDays);
            DeleteByFilter(searchVO, isLogicalDelete: false);
        }
    }
}
