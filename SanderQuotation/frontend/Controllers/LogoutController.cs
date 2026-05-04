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

            Response.Cookies.Delete(Const.Value.JWT_TokenName, new CookieOptions
            {
                Path = "/",
                Domain = domain,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            Response.Cookies.Append(
               CookieRequestCultureProvider.DefaultCookieName,
               CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US")),
               new CookieOptions
               {
                   Expires = DateTimeOffset.UtcNow.AddYears(1),
                   Domain = Common.Method.GetAppSettingsDataByName("frontendDoamin"),   // ? 這行關鍵
                   HttpOnly = true,
                   Secure = true,                 // ?? 必須 true
                   SameSite = SameSiteMode.None,  // ?? 跨站一定要 None
                   Path = "/"
               }
           );

            return View("Logout");
        }
    }
}
