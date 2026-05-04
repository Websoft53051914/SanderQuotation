using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace frontend.Common
{
    public class FrontendSystemLockFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var isLocked =
                context.HttpContext.Session.GetString("SystemLocked") == "true";

            var controller =
                context.RouteData.Values["controller"]?.ToString();

            if (context.RouteData.Values["controller"]?.ToString().ToLower() == "login" && context.RouteData.Values["action"]?.ToString().ToLower() == "index")
            {
                //可進入登入頁面，同時進行登出行為
                var domain = Common.Method.GetAppSettingsDataByName("frontendDoamin");
#if DEBUG
                domain = "";
#endif

                context.HttpContext.Response.Cookies.Delete(Const.Value.JWT_TokenName, new CookieOptions
                {
                    Path = "/",
                    Domain = domain,
                    Secure = true,
                    SameSite = SameSiteMode.None
                });
            }
            else if (isLocked && controller != "Lock")
            {
                context.Result = new RedirectToActionResult(
                    "Index",
                    "Lock",
                    null
                );
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

}
