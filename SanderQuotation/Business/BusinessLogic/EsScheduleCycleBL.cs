using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Const;
using Core.Utility.Base.Data;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public partial class EsScheduleCycleBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public EsScheduleCycleBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var cfg = new MapperConfiguration(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<EsScheduleCycleEntity, EsScheduleCycleDM>().ReverseMap();
                c.CreateMap<EsTransferErrorLogDM, EsTransferErrorLogEntity>();
                c.CreateMap<EsTransferErrorLogEntity, EsTransferErrorLogDM>();
            });
            _mapper = cfg.CreateMapper();
        }

        public EsScheduleCycleBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IEsScheduleCycleDAO GetDao() => _unitOfWork.Repository<IEsScheduleCycleDAO>();
        private IEsScheduleCycleWeekDayDAO GetWeekDao() => _unitOfWork.Repository<IEsScheduleCycleWeekDayDAO>();
        private IEsScheduleCycleMonthDayDAO GetMonthDao() => _unitOfWork.Repository<IEsScheduleCycleMonthDayDAO>();
        private IEsScheduleCycleDbTransferDAO GetTransferDao() => _unitOfWork.Repository<IEsScheduleCycleDbTransferDAO>();
        private IEsScheduleCycleExcelDAO GetExcelDao() => _unitOfWork.Repository<IEsScheduleCycleExcelDAO>();
        private IEsScheduleCycleOtherTransferDAO GetOtherTransferDao() => _unitOfWork.Repository<IEsScheduleCycleOtherTransferDAO>();
    }

    public partial class EsScheduleCycleBL
    {
        // ── 列表查詢 ────────────────────────────────────────────
        public PageResult<EsScheduleCycleDM> GetPageList(PageEntity pageEntity, SearchVO query)
        {
            var pageResult = GetDao().GetPageList(pageEntity,query);

            var dms = _mapper.Map<List<EsScheduleCycleDM>>(pageResult.Results);

            FillSubTablesBatch(dms);

            return new PageResult<EsScheduleCycleDM>
            {
                CurrentPage = pageResult.CurrentPage,
                DataCount = pageResult.DataCount,
                PageDataSize = pageResult.PageDataSize,
                Results = dms
            };
        }

        // ── 單筆查詢 ────────────────────────────────────────────
        public EsScheduleCycleDM? Get(Guid rowGuid)
        {
            var entity = GetDao().FindByProperty(nameof(EsScheduleCycleEntity.Id), rowGuid);
            if (entity == null) return null;

            var dm = _mapper.Map<EsScheduleCycleDM>(entity);
            FillSubTables(dm);
            return dm;
        }

        public EsScheduleCycleDM? GetByCode(string code)
        {
            var entity = GetDao().FindByProperty(nameof(EsScheduleCycleEntity.ScheduleCycleCode), code);
            if (entity == null) return null;
            var dm = _mapper.Map<EsScheduleCycleDM>(entity);
            FillSubTables(dm);
            return dm;
        }

        public void CheckExist(EsScheduleCycleDM dm)
        {
            if (dm.Id == Guid.Empty)
            {
                var list = GetDao().GetListByFilter(new Const.SearchVO()
                {
                    ScheduleCycleCodeEq = dm.ScheduleCycleCode
                });
                if (list.Count > 0)
                {
                    GetMessage().SetAlert("此週期代碼已存在");
                }
            }
        }

        // ── 新增 ────────────────────────────────────────────────
        public void Create(EsScheduleCycleDM dm)
        {

            var entity = _mapper.Map<EsScheduleCycleEntity>(dm);
            //entity.RowGuid = Guid.NewGuid();
            entity.Status = dm.IsEnabled ? StatusEnum.Enabled.ToInt() : StatusEnum.Disabled.ToInt();
            entity.CreatedAt = base.now;
            entity.UpdatedAt = base.now;
            entity.CreatedBy = UserInfo?.UserAccount;
            entity.UpdatedBy = UserInfo?.UserAccount;

            GetDao().InsertAction(entity);
            SaveSubTables(dm, base.now);
            _unitOfWork.Commit();
        }

        // ── 編輯 ────────────────────────────────────────────────
        public void Edit(EsScheduleCycleDM dm)
        {

            var now = DateTime.Now;
            var entity = _mapper.Map<EsScheduleCycleEntity>(dm);
            // 保留建立資訊
            //entity.RowGuid = existing.RowGuid;
            entity.UpdatedAt = now;
            entity.UpdatedBy = UserInfo?.UserAccount;
            //
            entity.Status = dm.IsEnabled ? StatusEnum.Enabled.ToInt() : StatusEnum.Disabled.ToInt();

            GetDao().Update(entity);

            // 子表先全刪再重建
            DeleteSubTables(dm.ScheduleCycleCode);
            SaveSubTables(dm, now);
            _unitOfWork.Commit();
        }

        // ── 刪除（支援多筆）────────────────────────────────────
        public List<string> Delete(List<Guid> rowGuids)
        {
            List<string> codes = new();
            foreach (var rowGuid in rowGuids)
            {
                var entity = GetDao().FindByProperty(nameof(EsScheduleCycleEntity.Id), rowGuid);
                if (entity == null) continue;

                DeleteSubTables(entity.ScheduleCycleCode);
                entity.UpdatedAt = base.now;
                entity.UpdatedBy = UserInfo?.UserAccount;
                entity.Status = StatusEnum.Cancel.ToInt();
                GetDao().Update(entity);
                codes.Add(entity.ScheduleCycleCode);
            }

            _unitOfWork.Commit();
            return codes;
        }

        public List<EsScheduleCycleDM> GetAllActive()
        {
            var entities = GetDao().FindListByProperty(nameof(EsScheduleCycleEntity.Status), StatusEnum.Enabled.ToInt());
            var dms = _mapper.Map<List<EsScheduleCycleDM>>(entities);
            return dms;
        }


        // ── 私有輔助 ────────────────────────────────────────────
        private void FillSubTablesBatch(List<EsScheduleCycleDM> dms)
        {
            if (dms.Count == 0) return;

            var codes = dms.Select(x => x.ScheduleCycleCode).ToList();

            var weekDays = GetWeekDao().GetByCodes(codes).ToLookup(x => x.ScheduleCycleCode);
            var monthDays = GetMonthDao().GetByCodes(codes).ToLookup(x => x.ScheduleCycleCode);
            var transfers = GetTransferDao().GetByCodes(codes).ToLookup(x => x.ScheduleCycleCode);
            var excels = GetExcelDao().GetByCodes(codes).ToLookup(x => x.ScheduleCycleCode);
            var otherTransfers = GetOtherTransferDao().GetListbyFilter(new Const.SearchVO()
            {
                ScheduleCycleCodeIn = codes
            }).ToLookup(x => x.ScheduleCycleCode);

            foreach (var dm in dms)
            {
                var code = dm.ScheduleCycleCode;
                dm.WeekDays = weekDays[code].Select(x => x.WeekDay.ToString()).ToList();
                dm.MonthDays = monthDays[code].Select(x => x.MonthDay.ToString()).ToList();
                dm.DBTransferSettings = transfers[code].Select(x => x.TransferCode).ToList();
                dm.FileTransferSettings = excels[code].Select(x => x.TransferCode).ToList();
                dm.OtherTransferSettings = otherTransfers[code].Select(x => x.ActionType).ToList();
            }
        }

        private void FillSubTables(EsScheduleCycleDM dm)
        {
            var code = dm.ScheduleCycleCode;
            dm.WeekDays = GetWeekDao()
                .FindListByPropertys(new Dictionary<string, object>()
                {
                    {nameof(EsScheduleCycleWeekDayEntity.ScheduleCycleCode), code },
                    {nameof(EsScheduleCycleWeekDayEntity.Status), StatusEnum.Enabled.ToInt()  }
                })
                .Select(x => x.WeekDay.ToString()).ToList();

            dm.MonthDays = GetMonthDao()
                .FindListByPropertys(new Dictionary<string, object>()
                {
                    {nameof(EsScheduleCycleMonthDayEntity.ScheduleCycleCode), code },
                    {nameof(EsScheduleCycleMonthDayEntity.Status), StatusEnum.Enabled.ToInt()  }
                })
                .Select(x => x.MonthDay.ToString()).ToList();

            dm.DBTransferSettings = GetTransferDao()
                .FindListByPropertys(new Dictionary<string, object>()
                {
                    {nameof(EsScheduleCycleDbTransferEntity.ScheduleCycleCode), code },
                    {nameof(EsScheduleCycleDbTransferEntity.Status), StatusEnum.Enabled.ToInt()  }
                })
                .Select(x => x.TransferCode).ToList();

            dm.FileTransferSettings = GetExcelDao()
                .FindListByPropertys(new Dictionary<string, object>()
                {
                    {nameof(EsScheduleCycleFileTransferEntity.ScheduleCycleCode), code },
                    {nameof(EsScheduleCycleFileTransferEntity.Status), StatusEnum.Enabled.ToInt()  }
                })
                .Select(x => x.TransferCode).ToList();

            dm.OtherTransferSettings = GetOtherTransferDao()
                 .FindListByPropertys(new Dictionary<string, object>()
                {
                    {nameof(EsScheduleCycleFileTransferEntity.ScheduleCycleCode), code },
                    {nameof(EsScheduleCycleFileTransferEntity.Status), StatusEnum.Enabled.ToInt()  }
                })
                .Select(x => x.ActionType).ToList();
        }

        private void SaveSubTables(EsScheduleCycleDM dm, DateTime now)
        {
            var code = dm.ScheduleCycleCode;

            foreach (var wd in dm.WeekDays.Where(x => !string.IsNullOrEmpty(x)))
            {
                GetWeekDao().InsertAction(new EsScheduleCycleWeekDayEntity
                {
                    ScheduleCycleCode = code,
                    WeekDay = byte.Parse(wd),
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = UserInfo?.UserAccount,
                    UpdatedBy = UserInfo?.UserAccount,
                    Status = StatusEnum.Enabled.ToInt()
                });
            }

            foreach (var md in dm.MonthDays.Where(x => !string.IsNullOrEmpty(x)))
            {
                GetMonthDao().InsertAction(new EsScheduleCycleMonthDayEntity
                {
                    ScheduleCycleCode = code,
                    MonthDay = byte.Parse(md),
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = UserInfo?.UserAccount,
                    UpdatedBy = UserInfo?.UserAccount,
                    Status = StatusEnum.Enabled.ToInt()
                });
            }

            foreach (var tc in dm.DBTransferSettings.Where(x => !string.IsNullOrEmpty(x)))
            {
                GetTransferDao().InsertAction(new EsScheduleCycleDbTransferEntity
                {
                    ScheduleCycleCode = code,
                    TransferCode = tc,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = UserInfo?.UserAccount,
                    UpdatedBy = UserInfo?.UserAccount,
                    Status = StatusEnum.Enabled.ToInt()
                });
            }

            foreach (var ic in dm.FileTransferSettings.Where(x => !string.IsNullOrEmpty(x)))
            {
                GetExcelDao().InsertAction(new EsScheduleCycleFileTransferEntity
                {
                    ScheduleCycleCode = code,
                    TransferCode = ic,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = UserInfo?.UserAccount,
                    UpdatedBy = UserInfo?.UserAccount,
                    Status = StatusEnum.Enabled.ToInt()
                });
            }

            foreach (var ic in dm.OtherTransferSettings)
            {
                GetOtherTransferDao().InsertAction(new EsScheduleCycleOtherTransferEntity()
                {
                    ScheduleCycleCode = code,
                    ActionType = ic,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = UserInfo?.UserAccount,
                    UpdatedBy = UserInfo?.UserAccount,
                    Status = StatusEnum.Enabled.ToInt()
                });
            }

        }

        private void DeleteSubTables(string code)
        {
            GetWeekDao().FindListByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleWeekDayEntity.ScheduleCycleCode), code },{nameof(EsScheduleCycleWeekDayEntity.Status), StatusEnum.Enabled.ToInt() } })
                .ForEach(x =>
                {
                    x.UpdatedAt = base.now;
                    x.UpdatedBy = UserInfo?.UserAccount;
                    x.Status = StatusEnum.Cancel.ToInt();
                    GetWeekDao().Update(x);
                });
            GetMonthDao().FindListByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleMonthDayEntity.ScheduleCycleCode), code }, { nameof(EsScheduleCycleMonthDayEntity.Status), StatusEnum.Enabled.ToInt() } })
                .ForEach(x =>
                {
                    x.UpdatedAt = base.now;
                    x.UpdatedBy = UserInfo?.UserAccount;
                    x.Status = StatusEnum.Cancel.ToInt();
                    GetMonthDao().Update(x);
                });
            GetTransferDao().FindListByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleDbTransferEntity.ScheduleCycleCode), code }, { nameof(EsScheduleCycleDbTransferEntity.Status), StatusEnum.Enabled.ToInt() } })
                .ForEach(x =>
                {
                    x.UpdatedAt = base.now;
                    x.UpdatedBy = UserInfo?.UserAccount;
                    x.Status = StatusEnum.Cancel.ToInt();
                    GetTransferDao().Update(x);
                });
            GetExcelDao().FindListByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleFileTransferEntity.ScheduleCycleCode), code }, { nameof(EsScheduleCycleFileTransferEntity.Status), StatusEnum.Enabled.ToInt() } })
                .ForEach(x =>
                {
                    x.UpdatedAt = base.now;
                    x.UpdatedBy = UserInfo?.UserAccount;
                    x.Status = StatusEnum.Cancel.ToInt();
                    GetExcelDao().Update(x);
                });
                GetOtherTransferDao().FindListByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleOtherTransferEntity.ScheduleCycleCode), code }, { nameof(EsScheduleCycleOtherTransferEntity.Status), StatusEnum.Enabled.ToInt() } })
                .ForEach(x =>
                {
                    x.UpdatedAt = base.now;
                    x.UpdatedBy = UserInfo?.UserAccount;
                    x.Status = StatusEnum.Cancel.ToInt();
                    GetOtherTransferDao().Update(x);
                });
        }

        public List<(string Value, string Text)> GetDbTransferOptions()
        {
            var dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            return dao.FindListByProperty(nameof(ESDbTransferMappingEntity.Status),StatusEnum.Enabled.ToInt())
                .Select(x => (
                    Value: x.TransferMappingCode,
                    Text: x.TransferMappingCode
                )).ToList();
        }

        public List<(string Value, string Text)> GetFileTransferOptions()
        {
            var dao = _unitOfWork.Repository<IEsFileTransferMappingDAO>();
            return dao.FindListByProperty(nameof(EsFileTransferMappingEntity.Status), StatusEnum.Enabled.ToInt())
                .Select(x => (
                    Value: x.TransferMappingCode,
                    Text: x.TransferMappingCode
                )).ToList();
        }



        private static DispatcherReturnMsg Ok() => new() { IsSuccess = "Y", ReturnMsg = "" };
        private static DispatcherReturnMsg Fail(string msg) => new() { IsSuccess = "N", ReturnMsg = msg };
    }
}
