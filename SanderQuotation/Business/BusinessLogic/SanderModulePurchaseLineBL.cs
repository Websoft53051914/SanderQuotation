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
    /// 內部採購紀錄
    /// </summary>
    public partial class SanderModulePurchaseLineBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public SanderModulePurchaseLineBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<SanderModulePurchaseLineEntity, SanderModulePurchaseLineDM>().ReverseMap();
                c.CreateMap<SanderModulePurchaseLineDTO, SanderModulePurchaseLineDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public SanderModulePurchaseLineBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class SanderModulePurchaseLineBL
    {
        #region -- SanderModulePurchaseLine --

        private ISanderModulePurchaseLineDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ISanderModulePurchaseLineDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ISanderModulePurchaseLineDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<SanderModulePurchaseLineDM> GetListByFilter(SearchVO searchVO)
        {
            List<SanderModulePurchaseLineDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<SanderModulePurchaseLineDM> result = [];
            foreach (SanderModulePurchaseLineDTO item in dtoList)
            {
                SanderModulePurchaseLineDM dm = _mapper.Map<SanderModulePurchaseLineDM>(item);
                result.Add(dm);
            }

            return result;
        }
        #endregion
    }

    public partial class SanderModulePurchaseLineBL
    {
        /// <summary>
        /// 分頁查詢清單-價格分群
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁結果</returns>
        public PageResult<SanderModulePurchaseLineDM> GetPageListPriceCluster(PageEntity pageEntity, SearchVO searchVO)
        {
            PageResult<SanderModulePurchaseLineDTO> pageResult = GetDAO().GetPageListPriceCluster(pageEntity, searchVO);

            PageResult<SanderModulePurchaseLineDM> result = new();
            result.DataCount = pageResult.DataCount;
            result.Results = pageResult.Results.Select(x => _mapper.Map<SanderModulePurchaseLineDM>(x)).ToList();

            return result;
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoCreate(SanderModulePurchaseLineDM dm)
        {
            SanderModulePurchaseLineEntity entity = _mapper.Map<SanderModulePurchaseLineEntity>(dm);

            GetDAO().Insert(entity);
            dm.Id = entity.Id;
        }
    }

    /**
        private SanderModulePurchaseLineBL? _blSanderModulePurchaseLine = null;
        protected SanderModulePurchaseLineBL GetBLSanderModulePurchaseLine()
        {
            _blSanderModulePurchaseLine ??= new SanderModulePurchaseLineBL(_unitOfWork, SessionVO ?? new());
            _blSanderModulePurchaseLine._Configuration = _Configuration;

            return _blSanderModulePurchaseLine;
        }

        private SanderModulePurchaseLineBL? _blSanderModulePurchaseLine = null;

        /// <summary>
        /// 取得 SanderModulePurchaseLineBL 實例
        /// </summary>
        protected SanderModulePurchaseLineBL GetBlSanderModulePurchaseLine()
        {
            _blSanderModulePurchaseLine ??= GetBLInstance<SanderModulePurchaseLineBL>();

            return _blSanderModulePurchaseLine;
        }
     */
}
