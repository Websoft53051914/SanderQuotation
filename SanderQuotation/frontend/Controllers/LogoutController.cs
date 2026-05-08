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

            return View("Logout");
        }
    }
}
