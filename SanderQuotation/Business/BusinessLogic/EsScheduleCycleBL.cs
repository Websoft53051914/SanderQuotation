using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;

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
        private IEsScheduleCycleDbCsvTransferDAO GetDbCsvTransferDao() => _unitOfWork.Repository<IEsScheduleCycleDbCsvTransferDAO>();
        private IEsScheduleCycleExcelDAO GetExcelDao() => _unitOfWork.Repository<IEsScheduleCycleExcelDAO>();
    }

    public partial class EsScheduleCycleBL
    {
        // ── 列表查詢 ────────────────────────────────────────────
        public PageResult<EsScheduleCycleDM> GetPageList(CommonSearchQuery query)
        {
            var pageResult = GetDao().GetPageList(query);

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

        // ── 新增 ────────────────────────────────────────────────
        public DispatcherReturnMsg Create(EsScheduleCycleDM dm)
        {
            // 唯一碼檢查
            if (GetDao().IsExist(nameof(EsScheduleCycleEntity.ScheduleCycleCode), dm.ScheduleCycleCode))
                return Fail("");

            var now = DateTime.Now;
            var entity = _mapper.Map<EsScheduleCycleEntity>(dm);
            //entity.RowGuid = Guid.NewGuid();
            entity.Status = dm.IsEnabled ? 1 : 0;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.CreatedBy = UserInfo?.UserAccount;
            entity.UpdatedBy = UserInfo?.UserAccount;

            GetDao().InsertAction(entity);
            SaveSubTables(dm, now);
            _unitOfWork.Commit();

            return Ok();
        }

        // ── 編輯 ────────────────────────────────────────────────
        public DispatcherReturnMsg Edit(EsScheduleCycleDM dm)
        {
            var existing = GetDao().FindByProperty(nameof(EsScheduleCycleEntity.Id), dm.Id);
            if (existing == null) return Fail("");

            var now = DateTime.Now;
            var entity = _mapper.Map<EsScheduleCycleEntity>(dm);
            // 保留建立資訊
            //entity.RowGuid = existing.RowGuid;
            entity.SortNo = existing.SortNo;
            entity.Priority = existing.Priority;
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedAt = now;
            entity.UpdatedBy = UserInfo?.UserAccount;
            //
            entity.Status = dm.IsEnabled ? 1 : 0;

            GetDao().Update(entity);

            // 子表先全刪再重建
            DeleteSubTables(existing.ScheduleCycleCode);
            SaveSubTables(dm, now);
            _unitOfWork.Commit();

            return Ok();
        }

        // ── 刪除（支援多筆）────────────────────────────────────
        public (DispatcherReturnMsg, List<string>) Delete(List<Guid> rowGuids)
        {
            List<string> cycleCodes = new();
            foreach (var rowGuid in rowGuids)
            {
                var entity = GetDao().FindByProperty(nameof(EsScheduleCycleEntity.Id), rowGuid);
                if (entity == null) continue;

                DeleteSubTables(entity.ScheduleCycleCode);
                GetDao().Delete(rowGuid);
                cycleCodes.Add(entity.ScheduleCycleCode);
            }

            _unitOfWork.Commit();
            return (Ok(), cycleCodes);
        }

        public List<EsScheduleCycleDM> GetAllActive()
        {
            var entities = GetDao().FindListByProperty(nameof(EsScheduleCycleEntity.Status), "1");
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

            foreach (var dm in dms)
            {
                var code = dm.ScheduleCycleCode;
                dm.WeekDays = weekDays[code].Select(x => x.WeekDay.ToString()).ToList();
                dm.MonthDays = monthDays[code].Select(x => x.MonthDay.ToString()).ToList();
                dm.DBTransferSettings = transfers[code].Select(x => x.TransferCode).ToList();
                dm.FileTransferSettings = excels[code].Select(x => x.TransferCode).ToList();
            }
        }

        private void FillSubTables(EsScheduleCycleDM dm)
        {
            var code = dm.ScheduleCycleCode;
            dm.WeekDays = GetWeekDao()
                .FindListByProperty(nameof(EsScheduleCycleWeekDayEntity.ScheduleCycleCode), code)
                .Select(x => x.WeekDay.ToString()).ToList();

            dm.MonthDays = GetMonthDao()
                .FindListByProperty(nameof(EsScheduleCycleMonthDayEntity.ScheduleCycleCode), code)
                .Select(x => x.MonthDay.ToString()).ToList();

            dm.DBTransferSettings = GetTransferDao()
                .FindListByProperty(nameof(EsScheduleCycleDbTransferEntity.ScheduleCycleCode), code)
                .Select(x => x.TransferCode).ToList();

            dm.DbCsvTransferSettings = GetDbCsvTransferDao()
                .FindListByProperty(nameof(EsScheduleCycleDbCsvTransferEntity.ScheduleCycleCode), code)
                .Select(x => x.TransferCode).ToList();

            dm.FileTransferSettings = GetExcelDao()
                .FindListByProperty(nameof(EsScheduleCycleFileTransferEntity.ScheduleCycleCode), code)
                .Select(x => x.TransferCode).ToList();
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
                    UpdatedBy = UserInfo?.UserAccount
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
                    UpdatedBy = UserInfo?.UserAccount
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
                    UpdatedBy = UserInfo?.UserAccount
                });
            }

            foreach (var csv in dm.DbCsvTransferSettings.Where(x => !string.IsNullOrEmpty(x)))
            {
                GetDbCsvTransferDao().InsertAction(new EsScheduleCycleDbCsvTransferEntity
                {
                    ScheduleCycleCode = code,
                    TransferCode = csv,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = UserInfo?.UserAccount,
                    UpdatedBy = UserInfo?.UserAccount
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
                    UpdatedBy = UserInfo?.UserAccount
                });
            }
        }

        private void DeleteSubTables(string code)
        {
            GetWeekDao().DeleteByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleWeekDayEntity.ScheduleCycleCode), code } });
            GetMonthDao().DeleteByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleMonthDayEntity.ScheduleCycleCode), code } });
            GetTransferDao().DeleteByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleDbTransferEntity.ScheduleCycleCode), code } });
            GetDbCsvTransferDao().DeleteByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleDbCsvTransferEntity.ScheduleCycleCode), code } });
            GetExcelDao().DeleteByPropertys(new Dictionary<string, object> { { nameof(EsScheduleCycleFileTransferEntity.ScheduleCycleCode), code } });
        }

        public List<(string Value, string Text)> GetDbTransferOptions()
        {
            var dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            return dao.FindByAll()
                .Select(x => (
                    Value: x.TransferMappingCode,
                    Text: x.TransferMappingCode
                )).ToList();
        }

        public List<(string Value, string Text)> GetFileTransferOptions()
        {
            var dao = _unitOfWork.Repository<IEsFileTransferMappingDAO>();
            return dao.FindByAll()
                .Select(x => (
                    Value: x.TransferMappingCode,
                    Text: x.TransferMappingCode
                )).ToList();
        }



        private static DispatcherReturnMsg Ok() => new() { IsSuccess = "Y", ReturnMsg = "" };
        private static DispatcherReturnMsg Fail(string msg) => new() { IsSuccess = "N", ReturnMsg = msg };
    }
}
