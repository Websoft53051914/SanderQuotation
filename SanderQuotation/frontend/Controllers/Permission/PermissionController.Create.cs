using CommonClass.Models;
using frontend.Common;
using frontend.Common.Attribute;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

namespace frontend.Controllers.Permission
{
    public partial class PermissionController
    {
        //[CustomAuthorization(FuncID.Permission_Create)]
        public IActionResult Create()
        {
            List<SysRoleVM> vms = new List<SysRoleVM>()
            {
                new SysRoleVM(){ RoleName="系統管理員"},
                new SysRoleVM(){ RoleName="一般員工"},
            };

            ViewData["permissionList"] = vms;

            var accountStatusList = Method.GetAccountStatusForCreate();
            if (accountStatusList != null && accountStatusList.Count > 0)
                accountStatusList.FirstOrDefault().Selected = true;
            ViewData["accountStatusList"] = accountStatusList;
            return View("Edit");
        }

        private void SetEditViewInfo()
        {

        }
    }
}
