using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Business.BusinessLogic
{
    public partial class EsTransferErrorLogBL : BaseProjectBL
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public EsTransferErrorLogBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;

                cfg.CreateMap<EsTransferErrorLogDM, EsTransferErrorLogEntity>();
                cfg.CreateMap<EsTransferErrorLogEntity, EsTransferErrorLogDM>();

                cfg.CreateMap<EsTransferErrorLogDM, EsTransferErrorLogDTO>();
                cfg.CreateMap<EsTransferErrorLogDTO, EsTransferErrorLogDM>();
            });

            _mapper = configuration.CreateMapper();
        }

        public EsTransferErrorLogBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IEsTransferErrorLogDAO GetDAO() => _unitOfWork.Repository<IEsTransferErrorLogDAO>();

        public void DeleteOldLog(int days)
        {
            GetDAO().DeleteOldLog(days);
        }
    }
}
