using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Web.EX;
using DocumentFormat.OpenXml.EMMA;
using backend.Common;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class SysFuncController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public SysFuncController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SysFuncVM, SysFuncDM>().ReverseMap();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private SysFuncBL? _sysFuncBL;
        private SysFuncBL GetSysFuncBL()
        {
            _sysFuncBL ??= GetBLInstance<SysFuncBL>();
            return _sysFuncBL;
        }

        private SysFuncClassBL? _sysFuncClassBL;
        private SysFuncClassBL GetSysFuncClassBL()
        {
            _sysFuncClassBL ??= GetBLInstance<SysFuncClassBL>();
            return _sysFuncClassBL;
        }

        private SysFuncDetailBL? _sysFuncDetailBL;
        private SysFuncDetailBL GetSysFuncDetailBL()
        {
            _sysFuncDetailBL ??= GetBLInstance<SysFuncDetailBL>();
            return _sysFuncDetailBL;
        }

        /// <summary>
        /// 取得分頁列表
        /// </summary>
        [HttpGet("GetPageList")]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, SysFuncVM vm)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                var dm = _mapper.Map<SysFuncDM>(vm);
                var pageResult = GetSysFuncBL().GetPageList(pageEntity, dm);

                var list = _mapper.Map<List<SysFuncVM>>(pageResult.Results);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.No = (request.pageIndex - 1) * request.pageSize + i + 1;
                    item.StatusName = item.Status == 1 ? "啟用" : "停用";
                }

                return JsonSuccess(new
                {
                    Data = list,
                    Total = pageResult.DataCount
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        [HttpPost("Get")]
        public IActionResult Get(Guid id)
        {
            try
            {
                var dm = GetSysFuncBL().GetInfo(id);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                var vm = _mapper.Map<SysFuncVM>(dm);

                // 取得功能細項資料（權限代碼）
                var detailList = GetSysFuncDetailBL().GetInfoBySysFuncId(id);
                if (detailList != null && detailList.Count > 0)
                {
                    vm.FuncDetails = detailList.Select(d => new SysFuncDetailVM
                    {
                        Id = d.Id,
                        Name = d.Name,
                        FuncId = d.FuncId,
                        Sequence = d.Sequence,
                        PermissionCode = d.PermissionCode,
                        Status = d.Status
                    }).ToList();
                }

                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 新增
        /// </summary>
        [HttpPost("Create")]
        public IActionResult Create([FromBody] SysFuncVM vm)
        {
            try
            {
                // 驗證功能名稱是否已存在
                var existingFunc = GetSysFuncBL().CheckExist(vm.Name);
                if (existingFunc != null)
                {
                    return JsonValidFail("功能名稱已存在");
                }

                var dm = _mapper.Map<SysFuncDM>(vm);

                var guid = GetSysFuncBL().Create(dm);

                // 儲存功能細項（權限代碼）
                if (vm.PerCodes != null && vm.PerCodes.Count > 0)
                {
                    var detailDM = new SysFuncDetailDM
                    {
                        FuncId = dm.Id, // Guid 轉為 long（使用 HashCode）
                        PerCodes = vm.PerCodes,
                        Codes = vm.Codes ?? new List<string>()
                    };
                    detailDM.Id = dm.Id; // 用 Id 傳遞 FuncId（因為 Edit 方法使用 Id）
                    GetSysFuncDetailBL().Edit(detailDM);
                }

                LogSuccess(guid, LogAction.Create);
                return JsonSuccess("新增成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 編輯
        /// </summary>
        [HttpPost("Edit")]
        public IActionResult Edit([FromBody] SysFuncVM vm)
        {
            try
            {
                // 驗證功能名稱是否已存在（排除自己）
                var existingFunc = GetSysFuncBL().CheckExist(vm.Name);
                if (existingFunc != null && existingFunc.Id != vm.Id)
                {
                    return JsonValidFail("功能名稱已存在");
                }

                var dm = _mapper.Map<SysFuncDM>(vm);

                GetSysFuncBL().Edit(dm);

                // 更新功能細項（權限代碼）
                var detailDM = new SysFuncDetailDM
                {
                    Id = vm.Id,
                    PerCodes = vm.PerCodes ?? new List<string>(),
                    Codes = vm.Codes ?? new List<string>()
                };
                GetSysFuncDetailBL().Edit(detailDM);

                LogSuccess(vm.Id, LogAction.Edit);
                return JsonSuccess("編輯成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 刪除（批次）
        /// </summary>
        [HttpPost("Delete")]
        public IActionResult Delete(List<Guid> list)
        {
            try
            {
                if (list == null || list.Count == 0)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                foreach (var id in list)
                {
                    GetSysFuncBL().Delete(id);
                    LogSuccess(id, LogAction.Delete);
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
        /// 啟用/停用
        /// </summary>
        [HttpPost("Enable")]
        public IActionResult Enable(Guid id, int enable)
        {
            try
            {
                GetSysFuncBL().Enable(id, enable);
                LogSuccess(id, LogAction.Edit);
                return JsonSuccess(enable == 1 ? "啟用成功" : "停用成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得所有功能類別（下拉選單用）
        /// </summary>
        [HttpGet("GetFuncClassList")]
        public IActionResult GetFuncClassList()
        {
            try
            {
                var list = GetSysFuncClassBL().GetAll();
                return JsonSuccess(list.Select(x => new
                {
                    CodeID = x.Id.ToString(),
                    CodeName = x.ClassName
                }));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得系統功能清單（用於角色權限設定）
        /// </summary>
        [HttpGet("GetSysFuncList")]
        public IActionResult GetSysFuncList()
        {
            try
            {
                var list = GetSysFuncBL().GetAll();
                var result = list.Where(x => x.Status == 1).Select(x =>
                {
                    // 取得功能詳細資料（權限代碼清單）
                    var details = GetSysFuncDetailBL().GetInfoBySysFuncId(x.Id);
                    var allowActionList = details?.Select(d => new { Id = d.Id, Sequence = d.Sequence }).ToList();

                    return new
                    {
                        Name = x.Name,
                        ClassName = x.ClassName,
                        Status = x.Status,
                        AllowActionList = allowActionList
                    };
                }).ToList();

                return JsonSuccess(result);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
