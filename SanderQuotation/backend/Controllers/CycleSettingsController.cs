using AutoMapper;
using backend.Common;
using backend.MESSource;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using ViewModel;
namespace backend.Controllers
{
    [Route("api/cycle-settings")]
    public class CycleSettingsController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly HangfireSchedulerHelper _hangfireSchedulerHelper;
        private readonly IServiceScopeFactory _scopeFactory;

        public CycleSettingsController(IConfiguration config, HangfireSchedulerHelper hangfireSchedulerHelper, IServiceScopeFactory scopeFactory):base(config)
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
        public IActionResult GetPageList([FromQuery] CommonSearchQuery query)
        {
            try
            {
                var pageResult = GetBL().GetPageList(query);
                var list = _mapper.Map<List<EsScheduleCycleVM>>(pageResult.Results);
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].No = (query.Page - 1) * query.PageSize + i + 1;
                    list[i].IsEnabled = list[i].Status == "1";
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
        public IActionResult Get([FromQuery] Guid id)
        {
            try
            {
                var dm = GetBL().Get(id);
                if (dm == null)
                    return NotFound();
                var vm = _mapper.Map<EsScheduleCycleVM>(dm);
                vm.IsEnabled = vm.Status == "1";
                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                //return JsonValidFail(HandleError(ex));
                return JsonValidFail(ex.Message);
            }
        }

        // POST api/cycle-settings/Create
        [HttpPost("Create")]
        public IActionResult Create([FromBody] EsScheduleCycleVM vm)
        {
            try
            {
                var dm = _mapper.Map<EsScheduleCycleDM>(vm);
                var result = GetBL().Create(dm);
                if (result.IsSuccess == "Y")
                {
                    result.ReturnCode = _config[$"message:{CultureInfo.CurrentCulture.Name}:OKCode"];
                    result.ReturnMsg = _config[$"message:{CultureInfo.CurrentCulture.Name}:MSG_Insert_Success"];

                    if (vm.IsEnabled)
                        _hangfireSchedulerHelper.InsertRecurringJob(dm.ScheduleCycleCode, dm.CronExpression);

                    return JsonSuccess(result);
                }
                else
                {
                    return JsonValidFail(_config[$"message:{CultureInfo.CurrentCulture.Name}:ScheduleCycleCode"]);
                }

            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(ex.Message);
            }
        }

        // POST api/cycle-settings/Edit
        [HttpPost("Edit")]
        public IActionResult Edit([FromBody] EsScheduleCycleVM vm)
        {
            try
            {
                var dm = _mapper.Map<EsScheduleCycleDM>(vm);
                var result = GetBL().Edit(dm);

                if (result.IsSuccess == "Y")
                {
                    result.ReturnCode = _config[$"message:{CultureInfo.CurrentCulture.Name}:OKCode"];
                    result.ReturnMsg = _config[$"message:{CultureInfo.CurrentCulture.Name}:MSG_Update_Success"];

                    _hangfireSchedulerHelper.RemoveRecurringJob(dm.ScheduleCycleCode);
                    if (vm.IsEnabled)
                    {
                        _hangfireSchedulerHelper.InsertRecurringJob(dm.ScheduleCycleCode, dm.CronExpression);
                    }

                    return JsonSuccess(result);
                }
                else
                {
                    return JsonValidFail(_config[$"message:{CultureInfo.CurrentCulture.Name}:ScheduleCycleCode"]);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:System_Error"]);
            }
        }

        // POST api/cycle-settings/Delete
        [HttpPost("Delete")]
        public IActionResult Delete([FromBody] List<Guid> rowGuids)
        {
            try
            {
                if (rowGuids == null || rowGuids.Count == 0)
                    return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:Data_Not_Found"]);

                var result = GetBL().Delete(rowGuids);
                if (result.Item1.IsSuccess == "Y")
                {
                    result.Item1.ReturnCode = _config[$"message:{CultureInfo.CurrentCulture.Name}:OKCode"];
                    result.Item1.ReturnMsg = _config[$"message:{CultureInfo.CurrentCulture.Name}:MSG_Delete_Success"];
                    foreach (var code in result.Item2)
                    {
                        _hangfireSchedulerHelper.RemoveRecurringJob(code);
                    }
                }
                return JsonSuccess(result.Item1);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(ex.Message);
            }
        }


        [HttpGet("GetSelectList")]
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
                    dbCSVSelectList = new List<SelectListItem>
                    {
                       
                    }
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(ex.Message);
            }
        }

        [HttpPost("ExecuteNow/{rowGuid}")]
        public IActionResult ExecuteNow(Guid rowGuid)
        {
            try
            {
                var bl = GetBLInstance<EsScheduleCycleBL>();
                var dm = bl.Get(rowGuid);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var transferJob = scope.ServiceProvider.GetRequiredService<TransferJob>();
                        await transferJob.ExecuteTask(dm.ScheduleCycleCode, "Manual");
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                    }
                });

                return JsonSuccess(_config[$"message:{CultureInfo.CurrentCulture.Name}:Schedule_Triggered"]);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(ex.Message);
            }
        }
    }
}
