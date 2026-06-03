using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using frontend.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using static Const.Enums;

namespace frontend.Controllers.LoginRecord
{
    /// <summary>登入紀錄查詢前端 Controller。</summary>
    public partial class LoginRecordController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;

        /// <summary>功能說明：注入組態與 Hosting。</summary>
        /// <param name="configuration">輸入參數：IConfiguration。</param>
        /// <param name="hostingEnvironment">輸入參數：IWebHostEnvironment。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public LoginRecordController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }
    }

    public partial class LoginRecordController
    {
        /// <summary>功能說明：顯示登入紀錄查詢頁，並載入狀態篩選下拉。</summary>
        /// <returns>輸出參數：IActionResult，Views/LoginRecord/Index；ViewData 含 StatusList。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetSelectListHandler().GetSelectListEnum&lt;LogStatusEnum&gt;。
        /// 訊息內容及生成條件：紀錄清單由前端 AJAX 載入後端 API。
        /// </remarks>
        public IActionResult Index()
        {
            List<SelectListItem> statusList = GetSelectListHandler().GetSelectListEnum<LogStatusEnum>();
            ViewData["StatusList"] = statusList;
            return View();
        }
    }
}
