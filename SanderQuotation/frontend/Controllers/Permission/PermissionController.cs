using CommonClass.Models;
using frontend.Common;
using frontend.Common.Attribute;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using System.Web;
using static Const.Enums;

namespace frontend.Controllers.Permission
{
    /// <summary>帳號權限管理前端 Controller。</summary>
    public partial class PermissionController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;

        /// <summary>功能說明：注入組態與 Hosting 環境。</summary>
        /// <param name="configuration">輸入參數：IConfiguration。</param>
        /// <param name="hostingEnvironment">輸入參數：IWebHostEnvironment。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public PermissionController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        /// <summary>功能說明：顯示帳號權限清單頁，並準備帳號狀態篩選下拉與功能 Id。</summary>
        /// <returns>輸出參數：IActionResult，Views/Permission/Index。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：Method.GetAccountStatusForCreate、GetStatus；WebMethod.SetFuncIdAndClassName。
        /// 訊息內容及生成條件：ViewData accountStatusList 含「全部」；列表資料由頁面呼叫後端 API 載入。
        /// </remarks>
        //[CustomAuthorization(FuncID.Permission_View)]
        public IActionResult Index()
        {
            var accountStatusList = Method.GetAccountStatusForCreate();
            accountStatusList.AddRange(Method.GetStatus());
            accountStatusList = accountStatusList.OrderBy(s => s.Value).ToList();
            accountStatusList.Insert(0, new SelectListItem() { Text = "全部", Value = " " });
            ViewData["accountStatusList"] = accountStatusList.OrderBy(s => s.Value).ToList();
            WebMethod.SetFuncIdAndClassName(ViewData, HttpContext.Request);

            return View();
        }
    }
}
