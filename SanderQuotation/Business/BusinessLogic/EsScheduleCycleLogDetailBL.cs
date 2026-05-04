using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Business.BusinessLogic
{
    public partial class EsScheduleCycleLogDetailBL : BaseProjectBL
    {
        private IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public EsScheduleCycleLogDetailBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;

                cfg.CreateMap<EsScheduleCycleLogDetailDM, EsScheduleCycleLogDetailEntity>();
                cfg.CreateMap<EsScheduleCycleLogDetailEntity, EsScheduleCycleLogDetailDM>();

                cfg.CreateMap<EsScheduleCycleLogDetailDM, EsScheduleCycleLogDetailDTO>();
                cfg.CreateMap<EsScheduleCycleLogDetailDTO, EsScheduleCycleLogDetailDM>();
            });

            mapper = configuration.CreateMapper();
        }

        public EsScheduleCycleLogDetailBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IEsScheduleCycleLogDetailDAO dao;
        private IEsScheduleCycleLogDetailDAO GetDAO()
        {
            dao ??= _unitOfWork.Repository<IEsScheduleCycleLogDetailDAO>();
            return dao;
        }
    }
}

