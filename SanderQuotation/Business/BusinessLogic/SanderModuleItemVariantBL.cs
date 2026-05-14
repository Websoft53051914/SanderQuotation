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
    /// 各料品 Variant 資料，含客戶承認型號
    /// </summary>
    public partial class SanderModuleItemVariantBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public SanderModuleItemVariantBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<SanderModuleItemVariantEntity, SanderModuleItemVariantDM>().ReverseMap();
                c.CreateMap<SanderModuleItemVariantDTO, SanderModuleItemVariantDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public SanderModuleItemVariantBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class SanderModuleItemVariantBL
    {
        #region -- SanderModuleItemVariant --

        private ISanderModuleItemVariantDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ISanderModuleItemVariantDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ISanderModuleItemVariantDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<SanderModuleItemVariantDM> GetListByFilter(SearchVO searchVO)
        {
            List<SanderModuleItemVariantDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<SanderModuleItemVariantDM> result = [];
            foreach (SanderModuleItemVariantDTO item in dtoList)
            {
                SanderModuleItemVariantDM dm = _mapper.Map<SanderModuleItemVariantDM>(item);
                result.Add(dm);
            }

            return result;
        }

        #endregion
    }
}
