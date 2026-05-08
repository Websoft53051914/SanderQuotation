

using Core.Utility.Helper.Message;
using Core.Utility.Web.Base;
using frontend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace frontend.Controllers
{
    public class BaseProjectController : BaseController
    {
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
//            var Breadcrumb = context.HttpContext.Request.Query["Breadcrumb"].ToString();
//            var MenuCode = context.HttpContext.Request.Query["MenuCode"].ToString();
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
//            if (!string.IsNullOrEmpty(Breadcrumb))
//            {
//                Response.Cookies.Append("Breadcrumb", Breadcrumb);
//            }
//            if (!string.IsNullOrEmpty(MenuCode))
//            {
//                Response.Cookies.Append("MenuCode", MenuCode);
//            }


//            // 2. 判斷是否為首頁 (Home/Index)
//            if (controllerName == "Home" && actionName == "Index")
//            {
//                // 移除 Cookie (透過設定過期時間為過去來移除)
//                Response.Cookies.Delete("Breadcrumb");
//                Response.Cookies.Delete("MenuCode");

//            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// 從 JWT Claim 取得部門清單
        /// </summary>
        protected List<string> GetDeptList()
        {
            var raw = User.FindFirst("DeptList")?.Value ?? string.Empty;
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        #region -- Instance --

        private MessageHelper? _msgHelper = null;
        /// <summary>
        /// 錯誤訊息資訊
        /// </summary>
        /// <returns></returns>
        public MessageHelper GetMessage()
        {
            _msgHelper ??= new MessageHelper();
            return _msgHelper;
        }

        private SelectListHandler? _selectListHandler = null;
        /// <summary>
        /// SelectListHandler
        /// </summary>
        /// <returns></returns>
        public SelectListHandler GetSelectListHandler()
        {
            _selectListHandler ??= new SelectListHandler();
            return _selectListHandler;
        }

        #endregion  -- Instance --
         
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
