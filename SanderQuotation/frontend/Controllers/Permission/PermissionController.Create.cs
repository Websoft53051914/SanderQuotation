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
        /// <summary>功能說明：顯示新增帳號頁（共用 Edit 檢視），預設角色與帳號狀態選項。</summary>
        /// <returns>輸出參數：IActionResult，Views/Permission/Edit。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：Method.GetAccountStatusForCreate；暫存角色清單 ViewData permissionList。
        /// 訊息內容及生成條件：儲存成功/失敗訊息由前端呼叫後端 API 後顯示；本 Action 僅準備 ViewData。
        /// </remarks>
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

        /// <summary>功能說明：編輯頁共用初始化（目前為空實作，預留擴充）。</summary>
        /// <remarks>
        /// 參考功能名稱與用途：供 Create/Edit 共用。
        /// 訊息內容及生成條件：無。
        /// </remarks>
        private void SetEditViewInfo()
        {

        }
    }
}
