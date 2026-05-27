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
                cfg.CreateMap<EsScheduleCycleLogDetailDM, EsScheduleCycleLogDetailWithLogDTO>().ReverseMap();
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

        /// <summary>
        /// 以 EsScheduleCycleLogDetail 為主表，查詢 ScheduleCycleCode，並加上日期區間篩選
        /// </summary>
        public List<EsScheduleCycleLogDM> GetLogsByCode(string scheduleCycleCode, DateTime? dateFrom, DateTime? dateTo)
        {
            var details = GetDetailDAO().GetDetailsByCode(scheduleCycleCode, dateFrom, dateTo);
            var allErrors = GetErrorLogDAO().FindListByScheduleCode(scheduleCycleCode);

            var grouped = details
                .GroupBy(d => new
                {
                    d.ScheduleCycleLogId,
                    d.ScheduleCycleCode,
                    d.LogRunAt,
                    d.LogDurationMs,
                    d.LogStatus,
                    d.TriggerType
                })
                .OrderByDescending(g => g.Key.LogRunAt);

            var result = new List<EsScheduleCycleLogDM>();
            foreach (var g in grouped)
            {
                var logDm = new EsScheduleCycleLogDM
                {
                    Id = g.Key.ScheduleCycleLogId,
                    ScheduleCycleCode = g.Key.ScheduleCycleCode,
                    RunAt = g.Key.LogRunAt,
                    DurationMs = g.Key.LogDurationMs,
                    Status = g.Key.LogStatus,
                    TriggerType = g.Key.TriggerType,
                    Details = g.Select(d =>
                    {
                        var detailDm = _mapper.Map<EsScheduleCycleLogDetailDM>(d);
                        detailDm.ErrorLogs = allErrors
                            .Where(e => e.ScheduleCycleLogDetailId == d.Id)
                            .Select(e => _mapper.Map<EsTransferErrorLogDM>(e))
                            .ToList();
                        return detailDm;
                    }).ToList()
                };
                result.Add(logDm);
            }

            return result;
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
                    detailEntity.CreatedAt = base.now;
                    detailEntity.UpdatedAt = base.now;
                    GetDetailDAO().InsertAction(detailEntity);

                    foreach (var errlog in detail.ErrorLogs)
                    {
                        var errlogEntity = _mapper.Map<EsTransferErrorLogEntity>(errlog);
                        errlogEntity.ScheduleCycleLogDetailId = detailEntity.Id;
                        errlogEntity.CreatedAt = base.now;
                        errlogEntity.UpdatedAt = base.now;
                        GetErrorLogDAO().InsertAction(errlogEntity);
                    }
                }

                _unitOfWork.Commit();
                scope.Complete();
            }
        }

        /// <summary>
        /// 刪除 N 天前的排程執行紀錄 (esScheduleCycleLog)
        /// </summary>
        public void DeleteOldLog(int days)
        {
            GetDAO().DeleteOldLog(days);
        }

        /// <summary>
        /// 刪除 N 天前的排程執行明細紀錄 (esScheduleCycleLogDetail)
        /// </summary>
        public void DeleteOldLogDetail(int days)
        {
            GetDetailDAO().DeleteOldLog(days);
        }
    }
}
