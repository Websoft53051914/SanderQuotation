using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace frontend.Controllers
{
    /// <summary>歷史檔案管理前端 Controller。</summary>
    public class HistoryFileController : BaseProjectController
    {
        private readonly IConfiguration _config;

        /// <summary>功能說明：注入組態（供 View 或 Helper 讀取 BackendURL 等）。</summary>
        /// <param name="config">輸入參數：IConfiguration。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController。
        /// 訊息內容及生成條件：建構子無 HTTP 回應。
        /// </remarks>
        public HistoryFileController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>功能說明：顯示歷史檔案清單與上傳管理頁。</summary>
        /// <returns>輸出參數：IActionResult，Views/HistoryFile/Index。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：頁面呼叫 backend HistoryFile API（分頁、上傳、下載）。
        /// 訊息內容及生成條件：操作結果訊息由後端 API 回傳；本 Action 僅渲染殼層。
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }
    }
}
