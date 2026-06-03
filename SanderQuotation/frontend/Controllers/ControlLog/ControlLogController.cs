using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using frontend.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using static Const.Enums;

namespace frontend.Controllers.ControlLog
{
    /// <summary>操作/控制日誌查詢前端 Controller。</summary>
    public partial class ControlLogController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;

        /// <summary>功能說明：注入組態與 Hosting。</summary>
        /// <param name="configuration">輸入參數：IConfiguration。</param>
        /// <param name="hostingEnvironment">輸入參數：IWebHostEnvironment。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public ControlLogController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }
    }

    public partial class ControlLogController
    {
        /// <summary>功能說明：顯示控制日誌查詢頁，並載入日誌狀態篩選下拉。</summary>
        /// <returns>輸出參數：IActionResult，Views/ControlLog/Index；ViewData 含 StatusList、IsWarning。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetSelectListHandler().GetSelectListEnum&lt;LogStatusEnum&gt;。
        /// 訊息內容及生成條件：清單資料由頁面呼叫後端 API；IsWarning 供前端標示警示樣式。
        /// </remarks>
        public IActionResult Index()
        {
            List<SelectListItem> statusList = GetSelectListHandler().GetSelectListEnum<LogStatusEnum>();
            ViewData["StatusList"] = statusList;
            ViewData["IsWarning"] = false;
            return View();
        }
    }
}
