using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Transactions;

namespace Business.BusinessLogic
{
    public partial class EsScheduleCycleLogBL : BaseProjectBL
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public EsScheduleCycleLogBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;

                cfg.CreateMap<EsScheduleCycleLogDM, EsScheduleCycleLogEntity>().ReverseMap();
                cfg.CreateMap<EsScheduleCycleLogDM, EsScheduleCycleLogDTO>().ReverseMap();
                cfg.CreateMap<EsScheduleCycleLogDetailDM, EsScheduleCycleLogDetailEntity>().ReverseMap();
                cfg.CreateMap<EsTransferErrorLogDM, EsTransferErrorLogEntity>().ReverseMap();

            });

            _mapper = configuration.CreateMapper();
        }

        public EsScheduleCycleLogBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IEsScheduleCycleLogDAO GetDAO() => _unitOfWork.Repository<IEsScheduleCycleLogDAO>();

        private IEsScheduleCycleLogDetailDAO GetDetailDAO() => _unitOfWork.Repository<IEsScheduleCycleLogDetailDAO>();

        private IEsTransferErrorLogDAO GetErrorLogDAO() => _unitOfWork.Repository<IEsTransferErrorLogDAO>();


        public List<EsScheduleCycleLogDM> GetLogsAfter(DateTime since)
        {
            var allLogs = GetDAO().FindByAll();
            var filtered = allLogs.Where(x => x.RunAt >= since).OrderByDescending(x => x.RunAt).ToList();
            var dms = _mapper.Map<List<EsScheduleCycleLogDM>>(filtered);

            // fill details
            var allDetails = GetDetailDAO().FindByAll();
            var allErrors = GetErrorLogDAO().FindByAll();

            foreach (var dm in dms)
            {
                var details = allDetails.Where(d => d.ScheduleCycleLogId == dm.Id).ToList();
                dm.Details = details.Select(d =>
                {
                    var detailDm = _mapper.Map<EsScheduleCycleLogDetailDM>(d);
                    var errors = allErrors.Where(e => e.ScheduleCycleLogDetailId == d.Id).ToList();
                    detailDm.ErrorLogs = _mapper.Map<List<EsTransferErrorLogDM>>(errors);
                    return detailDm;
                }).ToList();
            }

            return dms;
        }

        public EsScheduleCycleLogDetailDM GetDetailWithErrors(Guid detailId)
        {
            var detailEntity = GetDetailDAO().FindByProperty(nameof(EsScheduleCycleLogDetailEntity.Id), detailId);
            if (detailEntity == null) return null;

            var dm = _mapper.Map<EsScheduleCycleLogDetailDM>(detailEntity);
            var errors = GetErrorLogDAO().FindListByProperty(nameof(EsTransferErrorLogEntity.ScheduleCycleLogDetailId), detailId);
            dm.ErrorLogs = _mapper.Map<List<EsTransferErrorLogDM>>(errors);
            return dm;
        }

        public void InserLog(EsScheduleCycleLogDM dm)
        {
            using (var scope = new TransactionScope())
            {
                var entity = _mapper.Map<EsScheduleCycleLogEntity>(dm);
                entity.CreatedAt =base.now;
                entity.UpdatedAt =base.now;
                entity.CreatedBy = "service";
                entity.UpdatedBy = "service";
                GetDAO().InsertAction(entity);

                foreach (var detail in dm.Details)
                {
                    Guid detailId = Guid.NewGuid();
                    var detailEntity = _mapper.Map<EsScheduleCycleLogDetailEntity>(detail);
                    detailEntity.Id = detailId;
                    detailEntity.ScheduleCycleLogId = entity.Id;
                    GetDetailDAO().InsertAction(detailEntity);

                    foreach (var errlog in detail.ErrorLogs)
                    {
                        var errlogEntity = _mapper.Map<EsTransferErrorLogEntity>(errlog);
                        errlogEntity.ScheduleCycleLogDetailId = detailEntity.Id;
                        GetErrorLogDAO().InsertAction(errlogEntity);
                    }
                }

                _unitOfWork.Commit();
                scope.Complete();
            }
        }
    }
}
