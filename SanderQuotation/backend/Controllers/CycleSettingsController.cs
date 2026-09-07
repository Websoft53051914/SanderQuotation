using AutoMapper;
using backend.Common;
using Business.BusinessLogic;
using Sander.Platform.DbTransfer;
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

        /// <summary>
        /// 功能說明：建立排程週期設定 Controller，設定 AutoMapper 與 Hangfire 排程輔助。
        /// </summary>
        /// <param name="config">輸入參數：應用程式組態。</param>
        /// <param name="hangfireSchedulerHelper">輸入參數：Hangfire 週期工作註冊/移除/狀態查詢。</param>
        /// <param name="scopeFactory">輸入參數：背景工作 DI 範圍（ExecuteNow 用）。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController；EsScheduleCycleVM ↔ EsScheduleCycleDM 對應。
        /// 訊息內容及生成條件：建構子本身不產生 API 回應。
        /// </remarks>
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
        /// <summary>
        /// 功能說明：取得 EsScheduleCycleBL 單例。
        /// </summary>
        /// <returns>輸出參數：EsScheduleCycleBL。</returns>
        /// <remarks>參考功能名稱與用途：GetBLInstance。訊息內容及生成條件：無 HTTP 回應。</remarks>
        private EsScheduleCycleBL GetBL() => _bl ??= GetBLInstance<EsScheduleCycleBL>();

        /// <summary>
        /// 功能說明：分頁取得排程週期清單，並附 Hangfire 最近執行狀態。
        /// </summary>
        /// <param name="request">輸入參數：分頁與排序（ListPageEntity）。</param>
        /// <param name="Keyword">輸入參數：關鍵字模糊搜尋。</param>
        /// <returns>輸出參數：JsonSuccess({ Data, Total, Page, PageSize })。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetPageEntity、GetBL().GetPageList、HangfireSchedulerHelper.GetJobStatuses。
        /// 訊息內容及生成條件：成功 → 清單含 LastRunAt/State；例外 → JsonValidFail(System_Error 或語系 message)。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：依 Id 取得單筆排程週期設定。
        /// </summary>
        /// <param name="id">輸入參數：排程主鍵 Guid。</param>
        /// <returns>輸出參數：JsonSuccess(EsScheduleCycleVM)；dm 為 null 時 NotFound()。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetBL().Get、_mapper.Map。
        /// 訊息內容及生成條件：找不到 → NotFound；例外 → JsonValidFail(ex.Message)。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：新增排程週期；啟用時註冊 Hangfire Recurring Job。
        /// </summary>
        /// <param name="vm">輸入參數：排程設定 VM（含 Cron、IsEnabled 等）。</param>
        /// <returns>輸出參數：JsonSuccess「新增成功」；CheckExist 失敗或例外時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：CheckExist、Create、InsertRecurringJob。
        /// 訊息內容及生成條件：BL 錯誤 → JsonValidFail(GetErrMsg)；成功 →「新增成功」；例外 → System_Error。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：編輯排程週期；先移除再依 IsEnabled 重新註冊 Hangfire Job。
        /// </summary>
        /// <param name="vm">輸入參數：排程設定 VM。</param>
        /// <returns>輸出參數：JsonSuccess「編輯成功」；例外時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：Edit、RemoveRecurringJob、InsertRecurringJob。
        /// 訊息內容及生成條件：成功 →「編輯成功」；例外 → System_Error。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：批次刪除排程週期，並移除對應 Hangfire Recurring Job。
        /// </summary>
        /// <param name="rowGuids">輸入參數：要刪除的 Guid 清單。</param>
        /// <returns>輸出參數：JsonSuccess「刪除成功」。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetBL().Delete、RemoveRecurringJob（逐 Code）。
        /// 訊息內容及生成條件：成功 →「刪除成功」；例外 → System_Error。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：取得新增/編輯排程時的下拉選項（DB 轉檔、檔案轉檔等）。
        /// </summary>
        /// <returns>輸出參數：JsonSuccess({ dbSelectList, fileSelectList, dbCSVSelectList })。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetDbTransferOptions、GetFileTransferOptions。
        /// 訊息內容及生成條件：成功 → 選項清單；例外 → JsonValidFail(ex.Message)。
        /// </remarks>
        [HttpGet("GetSelectList")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_Create, Const.Enums.FuncID.Cyclesettings_Edit)]
        public IActionResult GetSelectList()
        {
            try
            {
                var bl = GetBLInstance<EsScheduleCycleBL>();
                var dbOptions = Business.BusinessFactory.GetInstance<IDbTransferService>().GetDbTransferOptions();
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

        /// <summary>
        /// 功能說明：立即手動觸發指定排程（Enqueue TransferJob，TriggerType=Manual）。
        /// </summary>
        /// <param name="rowGuid">輸入參數：排程資料列 Guid。</param>
        /// <returns>輸出參數：JsonSuccess「排程已觸發」。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：Get(rowGuid)、BackgroundJob.Enqueue&lt;TransferJob&gt;。
        /// 訊息內容及生成條件：成功 →「排程已觸發」；背景 Enqueue 失敗僅 LogError；API 層 catch → ex.Message。
        /// </remarks>
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
        /// 功能說明：取得指定排程執行紀錄（含 Detail、ErrorLog），支援日期區間篩選。
        /// </summary>
        /// <param name="scheduleCycleCode">輸入參數：排程代碼。</param>
        /// <param name="dateFrom">輸入參數：起始日期（可選）。</param>
        /// <param name="dateTo">輸入參數：結束日期（可選）。</param>
        /// <returns>輸出參數：JsonSuccess({ DateFrom, DateTo, Data: logs })。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：EsScheduleCycleLogBL.GetLogsByCode。
        /// 訊息內容及生成條件：成功 → 紀錄清單；例外 → System_Error。
        /// </remarks>
        [HttpGet("GetRecentLogs")]
        [CustomAuthorization(Const.Enums.FuncID.Cyclesettings_View)]
        public IActionResult GetRecentLogs([FromQuery] string scheduleCycleCode, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            try
            {
                var today = DateTime.Today;

                var bl = GetBLInstance<EsScheduleCycleLogBL>();
                var logs = bl.GetLogsByCode(scheduleCycleCode, dateFrom, dateTo)
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

                return JsonSuccess(new
                {
                    DateFrom = dateFrom?.ToString("yyyy-MM-dd"),
                    DateTo = dateTo?.ToString("yyyy-MM-dd"),
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
