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
using backend.Common.Attribute;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AccountController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<AccountVM, AccountDM>().ReverseMap();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private AccountBL? _accountBL;
        private AccountBL GetAccountBL()
        {
            _accountBL ??= GetBLInstance<AccountBL>();
            return _accountBL;
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
        [CustomAuthorization(FuncID.Permission_View)]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, AccountVM vm)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                var dm = _mapper.Map<AccountDM>(vm);
                var pageResult = GetAccountBL().GetPageList(pageEntity, dm);

                var list = _mapper.Map<List<AccountVM>>(pageResult.Results);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.No = (request.pageIndex - 1) * request.pageSize + i + 1;

                    if (item.RoleNames.Count > 0)
                    {
                        item.RoleName = string.Join(",", item.RoleNames);
                    }
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
        [CustomAuthorization(FuncID.Permission_View)]
        public IActionResult Get(Guid id)
        {
            try
            {
                var dm = GetAccountBL().GetInfo(id);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                var vm = _mapper.Map<AccountVM>(dm);
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
        [CustomAuthorization(FuncID.Permission_Create)]
        public IActionResult Create(AccountVM vm)
        {
            try
            {
                // 檢查帳號是否已存在
                var existAccount = GetAccountBL().CheckExist(vm.MemberAccount);
                if (existAccount != null)
                {
                    return JsonValidFail("帳號已存在");
                }

                var dm = _mapper.Map<AccountDM>(vm);
                var guid = GetAccountBL().Create(dm);
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
        [CustomAuthorization(FuncID.Permission_Edit)]
        public IActionResult Edit(AccountVM vm)
        {
            try
            {
                var dm = _mapper.Map<AccountDM>(vm);
                GetAccountBL().Edit(dm);
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
        [CustomAuthorization(FuncID.Permission_Delete)]
        public IActionResult Delete(List<Guid> list)
        {
            try
            {
                if (list == null || list.Count == 0)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                foreach (var id in list)
                {
                    GetAccountBL().Delete(id);
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
        [CustomAuthorization(FuncID.Permission_Edit)]
        public IActionResult Enable(Guid id, int enable)
        {
            try
            {
                GetAccountBL().Enable(id, enable);
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
        /// 取得所有角色列表（用於下拉選單）
        /// </summary>
        [HttpGet("GetRoleList")]
        [CustomAuthorization(FuncID.Permission_View)]
        public IActionResult GetRoleList()
        {
            try
            {
                var roles = GetSysRoleBL().GetAllRoles();

                return JsonSuccess(roles.Select(r => new
                {
                    CodeID = r.Id.ToString(),
                    CodeName = r.RoleName
                }));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
