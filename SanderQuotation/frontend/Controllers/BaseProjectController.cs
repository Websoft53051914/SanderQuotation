

using Core.Utility.Helper.Message;
using Core.Utility.Web.Base;
using frontend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace frontend.Controllers
{
    /// <summary>專案前端 MVC 基底 Controller。</summary>
    public class BaseProjectController : BaseController
    {
        /// <summary>功能說明：Action 執行前設定 ViewData 使用者資訊、麵包屑/選單 Cookie，首頁時清除 Cookie。</summary>
        /// <param name="context">輸入參數：ActionExecutingContext（含 RouteData、HttpContext）。</param>
        /// <remarks>
        /// 參考功能名稱與用途：User.FindFirst — 讀取 JWT Claims；Response.Cookies — 寫入 Breadcrumb、MenuCode。
        /// 訊息內容及生成條件：無 HTTP JSON；Home/Index 時 Delete Breadcrumb、MenuCode Cookie。
        /// </remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewData["name"] = User.FindFirst("UserName")?.Value;
            ViewData["acc"] = User.FindFirst("UserAccount")?.Value;

            var routeData = context.RouteData.Values;

            // 1. 取得目前的 Controller 和 Action 名稱
            string controllerName = routeData["controller"]?.ToString();
            string actionName = routeData["action"]?.ToString();

            //            var SystemCode = context.HttpContext.Request.Query["SystemCode"].ToString();
            //            var ModuleCode = context.HttpContext.Request.Query["ModuleCode"].ToString();
            var Breadcrumb = context.HttpContext.Request.Query["Breadcrumb"].ToString();
            var MenuCode = context.HttpContext.Request.Query["MenuCode"].ToString();
            //            if (!string.IsNullOrEmpty(SystemCode))
            //            {
            //#if DEBUG
            //                Response.Cookies.Append("SystemCode", SystemCode);
            //#endif
            //                Response.Cookies.Append("SystemCode", SystemCode, new CookieOptions
            //                {
            //                    Domain = Common.Method.GetAppSettingsDataByName("frontendDoamin"),   // ⭐ 這行關鍵
            //                    HttpOnly = true,
            //                    Secure = true,                 // 🔴 必須 true
            //                    SameSite = SameSiteMode.None,  // 🔴 跨站一定要 None
            //                    Path = "/"
            //                });
            //            }
            //            if (!string.IsNullOrEmpty(ModuleCode))
            //            {
            //#if DEBUG
            //                Response.Cookies.Append("ModuleCode", ModuleCode);

            //#endif
            //                Response.Cookies.Append("ModuleCode", ModuleCode, new CookieOptions
            //                {
            //                    Domain = Common.Method.GetAppSettingsDataByName("frontendDoamin"),   // ⭐ 這行關鍵
            //                    HttpOnly = true,
            //                    Secure = true,                 // 🔴 必須 true
            //                    SameSite = SameSiteMode.None,  // 🔴 跨站一定要 None
            //                    Path = "/"
            //                });
            //            }
            if (!string.IsNullOrEmpty(Breadcrumb))
            {
                Response.Cookies.Append("Breadcrumb", Breadcrumb);
            }
            if (!string.IsNullOrEmpty(MenuCode))
            {
                Response.Cookies.Append("MenuCode", MenuCode);
            }


            // 2. 判斷是否為首頁 (Home/Index)
            if (controllerName == "Home" && actionName == "Index")
            {
                // 移除 Cookie (透過設定過期時間為過去來移除)
                Response.Cookies.Delete("Breadcrumb");
                Response.Cookies.Delete("MenuCode");
            }

            base.OnActionExecuting(context);
        }

        /// <summary>功能說明：從 JWT Claim 取得部門代碼清單。</summary>
        /// <returns>輸出參數：部門代碼 List（以逗號分隔 Claim 解析）。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：User.FindFirst("DeptList")。
        /// 訊息內容及生成條件：Claim 空則回傳空清單。
        /// </remarks>
        protected List<string> GetDeptList()
        {
            var raw = User.FindFirst("DeptList")?.Value ?? string.Empty;
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        #region -- Instance --

        private MessageHelper? _msgHelper = null;
        /// <summary>功能說明：取得訊息輔助類別（累積驗證/業務錯誤）。</summary>
        /// <returns>輸出參數：MessageHelper 單例。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：供子頁面或 Helper 組合錯誤訊息。
        /// 訊息內容及生成條件：由呼叫端透過 Helper 設定，非直接 HTTP 回應。
        /// </remarks>
        public MessageHelper GetMessage()
        {
            _msgHelper ??= new MessageHelper();
            return _msgHelper;
        }

        private SelectListHandler? _selectListHandler = null;
        /// <summary>功能說明：取得下拉選單資料處理器（Enum、遠端選項等）。</summary>
        /// <returns>輸出參數：SelectListHandler 單例。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：各 Index/Edit 頁面填入 ViewData 選項清單。
        /// 訊息內容及生成條件：無 HTTP 回應。
        /// </remarks>
        public SelectListHandler GetSelectListHandler()
        {
            _selectListHandler ??= new SelectListHandler();
            return _selectListHandler;
        }

        #endregion  -- Instance --
         
        /// <summary>功能說明：將例外寫入 Trace（開發除錯用，非寫入 DB）。</summary>
        /// <param name="ex">輸入參數：例外物件。</param>
        /// <remarks>
        /// 參考功能名稱與用途：System.Diagnostics.Trace。
        /// 訊息內容及生成條件：輸出 Source、Message 至 Trace，無使用者可見 JSON。
        /// </remarks>
        protected void LogError(Exception ex)
        {
            Trace.Write("<font color=red>Source:" + ex.Source + "</font>");
            Trace.Write("<font color=red>Msg:" + ex.Message + "</font>");
            Trace.Write(ex.ToString());
        }


        /// <summary>
        /// 紀錄例外於資料庫
        /// </summary>
        /// <param name="ex"></param>
        /// <returns>Control_Log.Id</returns>
        //protected long LogError(Exception ex)
        //{
        //    var blLog = BLFactory.GetInstance<LogBL>();

        //    var logDM = new Control_LogDM()
        //    {
        //        IP = LoginSession.Current.IP ?? Method.GetClientIPAddress(),
        //        Status = ((int)LogStatusEnum.Failed).ToString(),
        //        ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
        //        ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
        //        Exception = ex.ToString()
        //    };

        //    return blLog.InsertLog(logDM);
        //}

        /// <summary>
        /// 紀錄失敗訊息於資料庫
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>Control_Log.Id</returns>
        //protected long LogError(string exception)
        //{
        //    var blLog = BLFactory.GetInstance<LogBL>();

        //    var logDM = new Control_LogDM()
        //    {
        //        IP = LoginSession.Current.IP ?? Method.GetClientIPAddress(),
        //        Status = ((int)LogStatusEnum.Failed).ToString(),
        //        ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
        //        ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
        //        Exception = exception
        //    };

        //    return blLog.InsertLog(logDM);
        //}

        /// <summary>
        /// 紀錄成功訊息於資料庫
        /// </summary>
        /// <param name="description"></param>
        //protected void LogSuccess(string description = null)
        //{
        //    var blLog = BLFactory.GetInstance<LogBL>();

        //    var logDM = new Control_LogDM()
        //    {
        //        IP = LoginSession.Current.IP ?? Method.GetClientIPAddress(),
        //        Status = ((int)LogStatusEnum.Success).ToString(),
        //        ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
        //        ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
        //        Exception = description,
        //    };

        //    blLog.InsertLog(logDM);
        //}

    }


}
