using DocumentFormat.OpenXml.EMMA;
using frontend.Common;
using frontend.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Diagnostics;

namespace frontend.Controllers
{
    public class HomeController : BaseProjectController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public class LoginDM
        {
            public Guid Id { set; get; }

            public Guid? EngineerId { set; get; }

            /// <summary>
            /// 會員帳號
            /// </summary>
            [Description("會員帳號")]
            public string MemberAccount { set; get; }

            /// <summary>
            /// 帳號名稱
            /// </summary>
            public string AccountName { set; get; }

            ///// <summary>
            ///// 角色權限FK
            ///// </summary>
            //public int PermissionID { set; get; }

            ///// <summary>
            ///// 帳號狀態，1:啟用 2:停用 3:開通中
            ///// </summary>
            public string AccountStatus { set; get; }

            ///// <summary>
            ///// 最後登入時間
            ///// </summary>
            //public string LastLoginTime { set; get; }
            //[Description("密碼")]
            public string MemberPWD { set; get; }
            public string ConFirmMemberPWD { set; get; }

            /// <summary>
            /// 驗證碼
            /// </summary>
            [Description("圖形驗證碼")]
            public string CaptchaCode { set; get; }

            /// <summary>
            /// 驗證碼的答案
            /// </summary>
            public string CaptchaCodeAnswer { set; get; }
             

            //public string LoginDateTime { set; get; }
            public string LastMemberPWDTime { set; get; }
            ////public int LoginTime { set; get; }
            public string AccountEMail { set; get; }
            public string ResetPWDCode { set; get; }
            public string LastForgetPWDTime { set; get; }



            public int LineSetting { get; set; }
            public string TempAccount { get; set; }
            public string? IP { get; set; }
            public int Logins { set; get; }
            public string LockTime { set; get; }
        }
        //[Authorize]
        public IActionResult Index()
        {
            var loginDM = new LoginDM()
            {
                MemberAccount = "admin",
            };
            Method.SetToSession(loginDM);

            return View();
        }

        public IActionResult ProfileSettings()
        {
            return View();
        }

        public IActionResult LockScreen()
        {
            return View();
        }

        public IActionResult PasswordChange()
        {
            return View();
        }
        public IActionResult PinCodeChange()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
#if DEBUG
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1),
                    
                }
            );
#endif
            Response.Cookies.Append(
               CookieRequestCultureProvider.DefaultCookieName,
               CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
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


            return LocalRedirect(returnUrl);
        }

    }
}
