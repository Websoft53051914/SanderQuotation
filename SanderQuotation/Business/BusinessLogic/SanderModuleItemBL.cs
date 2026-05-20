using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Business.BusinessLogic
{
    /// <summary>
    /// ERP 料號基本資料
    /// </summary>
    public partial class SanderModuleItemBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public SanderModuleItemBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<SanderModuleItemEntity, SanderModuleItemDM>().ReverseMap();
                c.CreateMap<SanderModuleItemDTO, SanderModuleItemDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public SanderModuleItemBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class SanderModuleItemBL
    {
        #region -- SanderModuleItem --

        private ISanderModuleItemDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ISanderModuleItemDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ISanderModuleItemDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<SanderModuleItemDM> GetListByFilter(SearchVO searchVO)
        {
            List<SanderModuleItemDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<SanderModuleItemDM> result = [];
            foreach (SanderModuleItemDTO item in dtoList)
            {
                SanderModuleItemDM dm = _mapper.Map<SanderModuleItemDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public SanderModuleItemDM? GetOneInfo(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            searchVO.IsLimit1 = true;

            return GetListByFilter(searchVO).FirstOrDefault();
        }

        /// <summary>
        /// 依採購型號取得單筆資料
        /// </summary>
        /// <param name="no">採購型號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public SanderModuleItemDM? GetOneInfoByNo(string no)
        {
            SearchVO searchVO = new();
            searchVO.SanderModuleItemNoEq = no;
            searchVO.IsLimit1 = true;

            return GetListByFilter(searchVO).FirstOrDefault();
        }

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        public void DeleteByFilter(SearchVO searchVO)
        {
            string account = SessionVO?.Account ?? string.Empty;
            GetDAO().DeleteByFilter(searchVO, account);
        }

        #endregion -- SanderModuleItem --
    }

    public partial class SanderModuleItemBL
    {
        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁 DM 清單</returns>
        public PageResult<SanderModuleItemDM> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            PageResult<SanderModuleItemDTO> pageResult = GetDAO().GetPageList(pageEntity, searchVO);

            List<SanderModuleItemDM> results = [];
            foreach (SanderModuleItemDTO item in pageResult.Results)
            {
                SanderModuleItemDM dm = _mapper.Map<SanderModuleItemDM>(item);
                results.Add(dm);
            }

            return new PageResult<SanderModuleItemDM>
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
        public void DoCreate(SanderModuleItemDM dm)
        {
            ArgumentNullException.ThrowIfNull(dm.No);
            SanderModuleItemDM? dmOld = GetOneInfoByNo(dm.No);

            if(dmOld == null)
            {
                SanderModuleItemEntity entity = _mapper.Map<SanderModuleItemEntity>(dm);

                GetDAO().Insert(entity);
                dm.Id = entity.Id;
            }
            else
            {
                SanderModuleItemEntity entity = GetDAO().FindByPk(dmOld.Id);
                entity.Description = dm.Description;
                entity.Description2 = dm.Description2;
                entity.LongDesc = dm.LongDesc;
                entity.LongDesc2 = dm.LongDesc2;
                GetDAO().Update(entity);
                _unitOfWork.Commit();
            }           
        }

        /// <summary>
        /// 取得指定批次數量的待處理料品（FlagNeedExtractKeyword = true）
        /// </summary>
        /// <param name="batchSize">批次筆數上限</param>
        /// <returns>待處理的 DM 清單</returns>
        public List<SanderModuleItemDM> GetListNeedExtractKeyword(int batchSize)
        {
            SearchVO searchVO = new();
            searchVO.SanderModuleItemFlagNeedExtractKeywordEq = true;
            searchVO.LimitRows = batchSize;

            return GetListByFilter(searchVO);
        }
    }

    /**
        private SanderModuleItemBL? _blSanderModuleItem = null;
        protected SanderModuleItemBL GetBLSanderModuleItem()
        {
            _blSanderModuleItem ??= new SanderModuleItemBL(_unitOfWork, SessionVO ?? new());
            _blSanderModuleItem._Configuration = _Configuration;

            return _blSanderModuleItem;
        }

        private SanderModuleItemBL? _blSanderModuleItem = null;

        /// <summary>
        /// 取得 SanderModuleItemBL 實例
        /// </summary>
        protected SanderModuleItemBL GetBlSanderModuleItem()
        {
            _blSanderModuleItem ??= GetBLInstance<SanderModuleItemBL>();

            return _blSanderModuleItem;
        }
     */
}
