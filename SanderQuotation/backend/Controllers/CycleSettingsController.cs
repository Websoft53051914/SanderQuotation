using AutoMapper;
using backend.Common;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Const;
using Core.Utility.Extensions;
using Core.Utility.Utility;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/cycle-settings")]
    public class CycleSettingsController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly HangfireSchedulerHelper _hangfireSchedulerHelper;
        private readonly IServiceScopeFactory _scopeFactory;

        public CycleSettingsController(IConfiguration config, HangfireSchedulerHelper hangfireSchedulerHelper, IServiceScopeFactory scopeFactory) : base(config)
        {
            _config = config;
            _hangfireSchedulerHelper = hangfireSchedulerHelper;
            _scopeFactory = scopeFactory;
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.CreateMap<EsScheduleCycleVM, EsScheduleCycleDM>()
                    .ForMember(d => d.WeekDays, o => o.MapFrom(s => s.WeekDays != null ? s.WeekDays.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() : new()))
                    .ForMember(d => d.MonthDays, o => o.MapFrom(s => s.MonthDays != null ? s.MonthDays.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() : new()));
                cfg.CreateMap<EsScheduleCycleDM, EsScheduleCycleVM>()
                    .ForMember(d => d.WeekDays, o => o.MapFrom(s => s.WeekDays != null ? string.Join(",", s.WeekDays) : ""))
                    .ForMember(d => d.MonthDays, o => o.MapFrom(s => s.MonthDays != null ? string.Join(",", s.MonthDays) : ""));
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private EsScheduleCycleBL? _bl;
        private EsScheduleCycleBL GetBL() => _bl ??= GetBLInstance<EsScheduleCycleBL>();

        // GET api/cycle-settings/GetPageList
        [HttpGet("GetPageList")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_View)]
        public IActionResult GetPageList([FromQuery] ListPageEntity request, string Keyword)
        {
            try
            {
                var pageEntity = base.GetPageEntity(request);
                var pageResult = GetBL().GetPageList(pageEntity, new SearchVO { KeywordLike = Keyword });
                var list = _mapper.Map<List<EsScheduleCycleVM>>(pageResult.Results);

                // 取得所有 Code 的即時 Hangfire 狀態（含 Processing）
                var codes = list.Select(x => x.ScheduleCycleCode).Where(c => c != null).Cast<string>();
                var jobStatuses = _hangfireSchedulerHelper.GetJobStatuses(codes);

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].No = (pageEntity.CurrentPage - 1) * pageEntity.PageDataSize + i + 1;
                    list[i].IsEnabled = list[i].Status == StatusEnum.Enabled.ToValueString();
                    list[i].OtherTransferDescriptionSettings = list[i].OtherTransferSettings
                        .Select(x => EnumUtility.GetDescriptionByInt<ScheduleCycleActionTypeEnum>(x))
                        .ToList();

                    if (jobStatuses.TryGetValue(list[i].ScheduleCycleCode, out var jobInfo))
                    {
                        list[i].LastRunAt = jobInfo.LastRunAt;
                        list[i].LastRunState = jobInfo.State.ToInt();
                        list[i].LastRunStateDescription = jobInfo.State.GetDescription();
                        list[i].LastRunMessage = jobInfo.LastJobId;
                    }
                }

                return JsonSuccess(new
                {
                    Data = list,
                    Total = pageResult.DataCount,
                    Page = pageResult.CurrentPage,
                    PageSize = pageResult.PageDataSize
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:System_Error"]);
            }
        }

        // GET api/cycle-settings/Get?id=...
        [HttpGet("Get")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_Edit)]
        public IActionResult Get([FromQuery] Guid id)
        {
            try
            {
                var dm = GetBL().Get(id);
                if (dm == null)
                    return NotFound();
                var vm = _mapper.Map<EsScheduleCycleVM>(dm);
                vm.IsEnabled = vm.Status == StatusEnum.Enabled.ToValueString();
                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(ex.Message);
            }
        }

        // POST api/cycle-settings/Create
        [HttpPost("Create")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_Create)]
        public IActionResult Create([FromBody] EsScheduleCycleVM vm)
        {
            try
            {
                var dm = _mapper.Map<EsScheduleCycleDM>(vm);
                GetBL().CheckExist(dm);
                if (GetBL().GetMessage().IsError())
                    return JsonValidFail(GetBL().GetMessage().GetErrMsg());

                GetBL().Create(dm);
                if (vm.IsEnabled)
                    _hangfireSchedulerHelper.InsertRecurringJob(dm.ScheduleCycleCode, dm.CronExpression);

                return JsonSuccess("新增成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // POST api/cycle-settings/Edit
        [HttpPost("Edit")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_Edit)]
        public IActionResult Edit([FromBody] EsScheduleCycleVM vm)
        {
            try
            {
                var dm = _mapper.Map<EsScheduleCycleDM>(vm);
                GetBL().Edit(dm);
                _hangfireSchedulerHelper.RemoveRecurringJob(dm.ScheduleCycleCode);
                if (vm.IsEnabled)
                    _hangfireSchedulerHelper.InsertRecurringJob(dm.ScheduleCycleCode, dm.CronExpression);

                return JsonSuccess("編輯成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // POST api/cycle-settings/Delete
        [HttpPost("Delete")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_Delete)]
        public IActionResult Delete([FromBody] List<Guid> rowGuids)
        {
            try
            {
                var codes = GetBL().Delete(rowGuids);
                foreach (var code in codes)
                {
                    _hangfireSchedulerHelper.RemoveRecurringJob(code);
                }

                return JsonSuccess("刪除成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpGet("GetSelectList")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_Create, Const.Enums.FuncID.Cyclesettings_Edit)]
        public IActionResult GetSelectList()
        {
            try
            {
                var bl = GetBLInstance<EsScheduleCycleBL>();
                var dbOptions = bl.GetDbTransferOptions();
                var fileOptions = bl.GetFileTransferOptions();

                return JsonSuccess(new
                {
                    dbSelectList = dbOptions.Select(o => new SelectListItem { Value = o.Value, Text = o.Text }).ToList(),
                    fileSelectList = fileOptions.Select(o => new SelectListItem { Value = o.Value, Text = o.Text }).ToList(),
                    dbCSVSelectList = new List<SelectListItem>()
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(ex.Message);
            }
        }

        [HttpPost("ExecuteNow/{rowGuid}")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_View)]
        public IActionResult ExecuteNow(Guid rowGuid)
        {
            try
            {
                var bl = GetBLInstance<EsScheduleCycleBL>();
                var dm = bl.Get(rowGuid);

                _ = Task.Run(() =>
                {
                    try
                    {
                        BackgroundJob.Enqueue<TransferJob>(
                            job => job.ExecuteTask(dm.ScheduleCycleCode, "Manual")
                        );
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                    }
                });

                return JsonSuccess("排程已觸發");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(ex.Message);
            }
        }

        /// <summary>
        /// 取得指定排程最近 24 小時的執行紀錄（含 Detail 和 ErrorLog）
        /// GET api/cycle-settings/GetRecentLogs?scheduleCycleCode=XXX
        /// </summary>
        [HttpGet("GetRecentLogs")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_View)]
        public IActionResult GetRecentLogs([FromQuery] string scheduleCycleCode)
        {
            try
            {
                var bl = GetBLInstance<EsScheduleCycleLogBL>();
                var logs = bl.GetLogsByCode(scheduleCycleCode)
                    .OrderByDescending(x => x.RunAt)
                    .Select(log => new
                    {
                        log.Id,
                        log.ScheduleCycleCode,
                        RunAt = log.RunAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        RunAtDate = log.RunAt.ToString("yyyy-MM-dd"),
                        RunAtTime = log.RunAt.ToString("HH:mm:ss"),
                        log.DurationMs,
                        log.Status,
                        log.TriggerType,
                        TotalDataCount = log.Details.Sum(d => d.DataCount),
                        TotalErrorCount = log.Details.Sum(d => d.ErrorCount),
                        Details = log.Details.Select(d => new
                        {
                            d.Id,
                            d.DBTransferCode,
                            d.FileTransferCode,
                            d.DataCount,
                            d.ErrorCount,
                            d.JobStatus,
                            d.DurationMs,
                            d.ErrorMessage,
                            TransferTypeDisplay = d.GetTransferTypeDisplay(),
                            TransferCode = d.GetTransferCode(),
                            ErrorLogs = d.ErrorLogs.Select(e => new
                            {
                                e.Id,
                                e.Sql,
                                e.Exception
                            }).ToList()
                        }).ToList()
                    }).ToList();

                return JsonSuccess(logs);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
