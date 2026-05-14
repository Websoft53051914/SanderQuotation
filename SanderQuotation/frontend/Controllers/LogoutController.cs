using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class LogoutController : BaseProjectController
    {
        private readonly ILogger<LogoutController> _logger;

        public LogoutController(ILogger<LogoutController> logger)
        {
            _logger = logger;
        }

        public IActionResult Logout()
        {
            var domain = Common.Method.GetAppSettingsDataByName("frontendDoamin");
#if DEBUG
            domain = "";
#endif
            var isHttps = Request.IsHttps;
            Response.Cookies.Delete(Const.Value.JWT_TokenName, new CookieOptions
            {
                Path = "/",
                //Domain = domain,
                Secure = isHttps,
                SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            });

            // 移除 Cookie (透過設定過期時間為過去來移除)
            Response.Cookies.Delete("Breadcrumb");
            Response.Cookies.Delete("MenuCode");

            return View("Logout");
        }
    }
}
