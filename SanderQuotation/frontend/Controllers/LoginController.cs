using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class LoginController : BaseProjectController
    {
        private readonly ILogger<HomeController> _logger;

        public LoginController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Force English language for Login page
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
            return View();
        }
        public IActionResult PinCodeReset()
        {
            // Force English language for PinCodeReset page
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
            return View();
        }
        public IActionResult TwoStepVerificationBasic()
        {
            // Force English language for TwoStepVerificationBasic page
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
            return View();
        }
    }
}
