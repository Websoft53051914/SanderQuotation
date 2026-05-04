using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using backend.Common;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class SysRoleController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public SysRoleController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SysRoleVM, SysRoleDM>().ReverseMap();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private SysRoleBL? _sysRoleBL;
        private SysRoleBL GetSysRoleBL()
        {
            _sysRoleBL ??= GetBLInstance<SysRoleBL>();
            return _sysRoleBL;
        }

        /// <summary>
        /// 取得分頁列表
        /// </summary>
        [HttpGet("GetPageList")]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, SysRoleVM vm)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                var dm = _mapper.Map<SysRoleDM>(vm);
                var pageResult = GetSysRoleBL().GetPageList(pageEntity, dm);

                var list = _mapper.Map<List<SysRoleVM>>(pageResult.Results);

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
                var dm = GetSysRoleBL().GetInfo(id);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                var vm = _mapper.Map<SysRoleVM>(dm);

                // 取得角色功能詳細ID清單
                vm.FuncDetailIds = GetSysRoleBL().GetFuncDetailIdsByRole(id);

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
        public IActionResult Create([FromBody] SysRoleVM vm)
        {
            try
            {
                // 檢查角色名稱是否已存在
                var existRole = GetSysRoleBL().CheckExist(vm.RoleName);
                if (existRole != null)
                {
                    return JsonValidFail("角色名稱已存在");
                }

                var dm = _mapper.Map<SysRoleDM>(vm);
                var guid = GetSysRoleBL().Create(dm);

                // 儲存功能權限設定
                if (vm.FuncDetailIds != null && vm.FuncDetailIds.Count > 0)
                {
                    // 取得新建立的角色ID
                    var newRole = GetSysRoleBL().CheckExist(vm.RoleName);
                    if (newRole != null)
                    {
                        newRole.PerCodes = vm.FuncDetailIds;
                        GetSysRoleBL().EditPermission(newRole);
                    }
                }

                LogSuccess(guid, LogAction.Edit);
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
        public IActionResult Edit([FromBody] SysRoleVM vm)
        {
            try
            {
                var dm = _mapper.Map<SysRoleDM>(vm);
                GetSysRoleBL().Edit(dm);

                // 儲存功能權限設定
                dm.PerCodes = vm.FuncDetailIds ?? new List<string>();
                GetSysRoleBL().EditPermission(dm);
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
                    GetSysRoleBL().Delete(id);
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
                GetSysRoleBL().Enable(id, enable);
                return JsonSuccess(enable == 1 ? "啟用成功" : "停用成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得所有功能列表（用於權限設定）
        /// </summary>
        [HttpGet("GetAllFuncList")]
        public IActionResult GetAllFuncList(Guid id)
        {
            try
            {
                var funcList = GetSysRoleBL().GetAllFuncList(id);
                return JsonSuccess(funcList);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 編輯權限
        /// </summary>
        [HttpPost("EditPermission")]
        public IActionResult EditPermission(SysRoleVM vm)
        {
            try
            {
                var dm = _mapper.Map<SysRoleDM>(vm);
                GetSysRoleBL().EditPermission(dm);
                return JsonSuccess("權限設定成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

    }
}
