using Core.Utility.Utility;
using frontend.Common;
using Microsoft.AspNetCore.Mvc;
using static Const.Enums;

namespace frontend.Controllers.CycleSettings
{
    /// <summary>排程週期設定前端 Controller。</summary>
    public partial class CycleSettingsController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;

        /// <summary>功能說明：注入組態與 Hosting 環境。</summary>
        /// <param name="configuration">輸入參數：IConfiguration。</param>
        /// <param name="hostingEnvironment">輸入參數：IWebHostEnvironment。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public CycleSettingsController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        /// <summary>功能說明：顯示排程週期清單與設定頁，並載入其他轉檔動作下拉選項。</summary>
        /// <returns>輸出參數：IActionResult，Views/CycleSettings/Index；ViewData 含 OtherTransferSettingSelectList。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetSelectListHandler().GetSelectListEnum&lt;ScheduleCycleActionTypeEnum&gt;；api/cycle-settings。
        /// 訊息內容及生成條件：清單與操作訊息由後端 API 回傳（如「排程已觸發」「刪除成功」）。
        /// </remarks>
        public IActionResult Index()
        {
            ViewData["OtherTransferSettingSelectList"] = GetSelectListHandler().GetSelectListEnum<ScheduleCycleActionTypeEnum>();
            return View();
        }
    }
}
