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
    /// 內部料號關鍵字資料模型
    /// </summary>
    public partial class TBSanderModuleItemKeywordBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public TBSanderModuleItemKeywordBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<TBSanderModuleItemKeywordEntity, TBSanderModuleItemKeywordDM>().ReverseMap();
                c.CreateMap<TBSanderModuleItemKeywordDTO, TBSanderModuleItemKeywordDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public TBSanderModuleItemKeywordBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class TBSanderModuleItemKeywordBL
    {
        #region -- TBSanderModuleItemKeyword --

        private ITBSanderModuleItemKeywordDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ITBSanderModuleItemKeywordDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ITBSanderModuleItemKeywordDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBSanderModuleItemKeywordDM> GetListByFilter(SearchVO searchVO)
        {
            List<TBSanderModuleItemKeywordDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<TBSanderModuleItemKeywordDM> result = [];
            foreach (TBSanderModuleItemKeywordDTO item in dtoList)
            {
                TBSanderModuleItemKeywordDM dm = _mapper.Map<TBSanderModuleItemKeywordDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public TBSanderModuleItemKeywordDM? GetOneInfo(Guid id)
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

        #endregion -- TBSanderModuleItemKeyword --

        public void DoInsert(TBSanderModuleItemKeywordDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            dm.Status = (int)StatusEnum.Enabled;
            dm.CreatedAt = nowTime;
            dm.CreatedBy = account;
            dm.UpdatedAt = nowTime;
            dm.UpdatedBy = account;
            TBSanderModuleItemKeywordEntity entity = _mapper.Map<TBSanderModuleItemKeywordEntity>(dm);
            GetDAO().InsertAction(entity);
            if (DoSaveChange)
            {
                _unitOfWork.Commit();
            }
        }
    }

    public partial class TBSanderModuleItemKeywordBL
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

            // 刪除舊關鍵字
            if (noList.Count > 0)
            {
                SearchVO deleteSearchVO = new();
                deleteSearchVO.SanderModuleItemNoIn = noList;

                GetDAO().DeleteByFilter(deleteSearchVO, account);
            }

            // 新增關鍵字
            foreach (TBSanderModuleItemKeywordDM dm in dmList)
            {
                DoInsert(dm);
            }

            // 更新 FlagNeedExtractKeyword
            if (ids.Count > 0)
            {
                GetBLSanderModuleItem().GetDAO().UpdateFlagNeedExtractKeyword(ids, false);
            }

            _unitOfWork.Commit();
        }
    }

    public partial class TBSanderModuleItemKeywordBL
    {
        /// <summary>
        /// 以文字相似度比對 LongDesc 關鍵字（用於 MPN / 廠牌比對）
        /// </summary>
        /// <param name="pKeyword">搜尋字串</param>
        /// <param name="pNo">限定料號（可為 null）</param>
        public List<TBSanderModuleItemKeywordDM> GetListMatchLongDesc(string pKeyword, string? pNo)
        {
            List<TBSanderModuleItemKeywordDTO> dtoList = GetDAO().GetListMatchLongDesc(pKeyword, pNo);

            List<TBSanderModuleItemKeywordDM> result = [];
            foreach (TBSanderModuleItemKeywordDTO item in dtoList)
            {
                TBSanderModuleItemKeywordDM dm = _mapper.Map<TBSanderModuleItemKeywordDM>(item);
                dm.SimilarityScore = item.SimilarityScore;
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 以向量相似度比對 Description 關鍵字
        /// </summary>
        /// <param name="pDescriptionVector">向量字串，格式 [x,y,...]</param>
        public List<TBSanderModuleItemKeywordDM> GetListMatchDescription(string pDescriptionVector)
        {
            List<TBSanderModuleItemKeywordDTO> dtoList = GetDAO().GetListMatchDescription(pDescriptionVector);

            List<TBSanderModuleItemKeywordDM> result = [];
            foreach (TBSanderModuleItemKeywordDTO item in dtoList)
            {
                TBSanderModuleItemKeywordDM dm = _mapper.Map<TBSanderModuleItemKeywordDM>(item);
                dm.SimilarityScore = item.SimilarityScore;
                result.Add(dm);
            }

            return result;
        }
    }

    public partial class TBSanderModuleItemKeywordBL
    {
        private SanderModuleItemBL? _blSanderModuleItem = null;
        protected SanderModuleItemBL GetBLSanderModuleItem()
        {
            _blSanderModuleItem ??= new SanderModuleItemBL(_unitOfWork, SessionVO ?? new());

            return _blSanderModuleItem;
        }
    }

    /**
        private TBSanderModuleItemKeywordBL? _blTBSanderModuleItemKeyword = null;
        protected TBSanderModuleItemKeywordBL GetBLTBSanderModuleItemKeyword()
        {
            _blTBSanderModuleItemKeyword ??= new TBSanderModuleItemKeywordBL(_unitOfWork, SessionVO ?? new());
            _blTBSanderModuleItemKeyword._Configuration = _Configuration;

            return _blTBSanderModuleItemKeyword;
        }
     */
}
