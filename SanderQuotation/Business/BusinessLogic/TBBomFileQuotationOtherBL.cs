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
    /// BOM 現貨優惠價結果
    /// </summary>
    public partial class TBBomFileQuotationOtherBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public TBBomFileQuotationOtherBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<TBBomFileQuotationOtherEntity, TBBomFileQuotationOtherDM>().ReverseMap();
                c.CreateMap<TBBomFileQuotationOtherDTO, TBBomFileQuotationOtherDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public TBBomFileQuotationOtherBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class TBBomFileQuotationOtherBL
    {
        #region -- TBBomFileQuotationOther --

        private ITBBomFileQuotationOtherDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ITBBomFileQuotationOtherDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ITBBomFileQuotationOtherDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileQuotationOtherDM> GetListByFilter(SearchVO searchVO)
        {
            List<TBBomFileQuotationOtherDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<TBBomFileQuotationOtherDM> result = [];
            foreach (TBBomFileQuotationOtherDTO item in dtoList)
            {
                TBBomFileQuotationOtherDM dm = _mapper.Map<TBBomFileQuotationOtherDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得啟用的資料清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileQuotationOtherDM> GetListEnabled(SearchVO searchVO)
        {
            searchVO.StatusEq = (int)Enums.StatusEnum.Enabled;

            return GetListByFilter(searchVO);
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public TBBomFileQuotationOtherDM? GetOneInfo(Guid id)
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
            {
                _unitOfWork.Commit();
            }
        }

        #endregion -- TBBomFileQuotationOther --

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="dm">DM 物件，BomFileContentId 與 SourceType 須已填妥</param>
        public void DoInsert(TBBomFileQuotationOtherDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            dm.Status = (int)Enums.StatusEnum.Enabled;
            dm.CreatedAt = nowTime;
            dm.CreatedBy = account;
            dm.UpdatedAt = nowTime;
            dm.UpdatedBy = account;

            TBBomFileQuotationOtherEntity entity = _mapper.Map<TBBomFileQuotationOtherEntity>(dm);
            GetDAO().InsertAction(entity);
            if (DoSaveChange)
            {
                _unitOfWork.Commit();
            }
        }

        /// <summary>
        /// 依已對應的 DM 新增一筆現貨優惠價（設定 BomFileContentId 與 SourceType 後存入）
        /// </summary>
        /// <param name="bomFileContentId">BOM 料項識別碼</param>
        /// <param name="sourceType">來源類型（對應 BomFileQuotationOtherSourceTypeEnum）</param>
        /// <param name="dm">已由呼叫端完成 VO→DM 對應的資料模型</param>
        public void DoInsertFromApiResult(Guid bomFileContentId, int sourceType, TBBomFileQuotationOtherDM dm)
        {
            dm.BomFileContentId = bomFileContentId;
            dm.SourceType = sourceType;
            DoInsert(dm);
        }
    }

    public partial class TBBomFileQuotationOtherBL
    {
    }
}

