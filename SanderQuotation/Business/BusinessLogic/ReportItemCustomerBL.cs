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
    /// 客戶代碼與 Variant Code 對照表
    /// </summary>
    public partial class ReportItemCustomerBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public ReportItemCustomerBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var cfg = new MapperConfiguration(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<ReportItemCustomerEntity, ReportItemCustomerDM>().ReverseMap();
                c.CreateMap<ReportItemCustomerDTO, ReportItemCustomerDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public ReportItemCustomerBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

    }

    public partial class ReportItemCustomerBL
    {
        #region -- ReportItemCustomer --

        private IReportItemCustomerDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public IReportItemCustomerDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<IReportItemCustomerDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<ReportItemCustomerDM> GetListByFilter(SearchVO searchVO)
        {
            List<ReportItemCustomerDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<ReportItemCustomerDM> result = [];
            foreach (ReportItemCustomerDTO item in dtoList)
            {
                ReportItemCustomerDM dm = _mapper.Map<ReportItemCustomerDM>(item);

                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public ReportItemCustomerDM? GetOneInfo(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            searchVO.IsLimit1 = true;

            ReportItemCustomerDM? dm = GetListByFilter(searchVO)
                .FirstOrDefault();

            return dm;
        }

        /// <summary>
        /// 取得啟用的資料清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<ReportItemCustomerDM> GetListEnabled(SearchVO searchVO)
        {
            return GetListByFilter(searchVO);
        }

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        public void DeleteByFilter(SearchVO searchVO)
        {
            string account = SessionVO?.Account ?? string.Empty;
            GetDAO().DeleteByFilter(searchVO, account);
        }

        #endregion -- ReportItemCustomer --
    }

    public partial class ReportItemCustomerBL
    {
        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoCreate(ReportItemCustomerDM dm)
        {
            ReportItemCustomerEntity entity = _mapper.Map<ReportItemCustomerEntity>(dm);

            GetDAO().Insert(entity);
            dm.Id = entity.Id;
        }
    }

    /**
        private ReportItemCustomerBL? _blReportItemCustomer = null;
        protected ReportItemCustomerBL GetBLReportItemCustomer()
        {
            _blReportItemCustomer ??= new ReportItemCustomerBL(_unitOfWork, SessionVO ?? new());
            _blReportItemCustomer._Configuration = _Configuration;

            return _blReportItemCustomer;
        }

        private ReportItemCustomerBL? _blReportItemCustomer = null;

        /// <summary>
        /// 取得 ReportItemCustomerBL 實例
        /// </summary>
        protected ReportItemCustomerBL GetBlReportItemCustomer()
        {
            _blReportItemCustomer ??= GetBLInstance<ReportItemCustomerBL>();

            return _blReportItemCustomer;
        }
     */
}
