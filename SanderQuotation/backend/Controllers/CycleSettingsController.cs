using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using backend.Common;
using backend.MESSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlX.XDevAPI.Common;
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
        private readonly TransferJob _transferJob;

        public CycleSettingsController(IConfiguration config, HangfireSchedulerHelper hangfireSchedulerHelper, TransferJob transferJob) : base(config)
        {
            _config = config;
            _hangfireSchedulerHelper = hangfireSchedulerHelper;
            _transferJob = transferJob;
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
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, EsScheduleCycleVM vm)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                IMapper mapper = CommonUtility.CreateMapper<EsScheduleCycleVM, EsScheduleCycleDM>();
                EsScheduleCycleDM dm = mapper.Map<EsScheduleCycleDM>(vm);
                var pageResult = GetBL().GetPageList(pageEntity, dm);
                var list = _mapper.Map<List<EsScheduleCycleVM>>(pageResult.Results);
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.No = (request.pageIndex - 1) * request.pageSize + i + 1;
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
                return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:System_Error"]);
            }
        }

        // POST api/cycle-settings/Create
        [HttpPost("Create")]
        public IActionResult Create([FromBody] EsScheduleCycleVM vm)
        {
            try
            {
                var dm = _mapper.Map<EsScheduleCycleDM>(vm);
                var guid = GetBL().Create(dm);
                LogSuccess(guid, LogAction.Create);
                return JsonSuccess("新增成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:System_Error"]);
            }
        }

        // POST api/cycle-settings/Edit
        [HttpPost("Edit")]
        public IActionResult Edit([FromBody] EsScheduleCycleVM vm)
        {
            try
            {
                var dm = _mapper.Map<EsScheduleCycleDM>(vm);
                GetBL().Edit(dm);
                LogSuccess(vm.Id, LogAction.Edit);
                return JsonSuccess("編輯成功");
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
                foreach (var item in rowGuids)
                {
                    LogSuccess(item, LogAction.Delete);
                }
                return JsonSuccess("刪除成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:System_Error"]);
            }
        }


        [HttpGet("GetSelectList")]
        public IActionResult GetSelectList()
        {
            try
            {
                var bl = GetBLInstance<EsScheduleCycleBL>();
                var dbOptions = bl.GetDbTransferOptions();
                //var fileOptions = bl.GetFileTransferOptions();

                return JsonSuccess(new
                {
                    dbSelectList = dbOptions.Select(o => new SelectListItem { Value = o.Value, Text = o.Text }).ToList(),
                    //fileSelectList = fileOptions.Select(o => new SelectListItem { Value = o.Value, Text = o.Text }).ToList()
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:System_Error"]);
            }
        }

        [HttpPost("ExecuteNow/{rowGuid}")]
        public IActionResult ExecuteNow(Guid rowGuid)
        {
            try
            {
                var bl = GetBLInstance<EsScheduleCycleBL>();
                var dm = bl.Get(rowGuid);
                _ = _transferJob.ExecuteTask(dm.ScheduleCycleCode, "Manual");
                return JsonSuccess(_config[$"message:{CultureInfo.CurrentCulture.Name}:Schedule_Triggered"]);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(_config[$"message:{CultureInfo.CurrentUICulture.Name}:System_Error"]);
            }
        }
    }
}
