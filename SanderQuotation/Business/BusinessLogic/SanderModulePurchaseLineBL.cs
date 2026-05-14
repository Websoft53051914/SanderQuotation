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
}
