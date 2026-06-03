using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers.TableExcel
{
    /// <summary>Excel/資料表匯入設定前端 Controller。</summary>
    public class TableExcelController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;

        /// <summary>功能說明：注入組態與 Web 根目錄（上傳範本路徑等）。</summary>
        /// <param name="configuration">輸入參數：IConfiguration。</param>
        /// <param name="hostingEnvironment">輸入參數：IWebHostEnvironment。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public TableExcelController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        /// <summary>功能說明：顯示匯入規則（TableExcel）清單與維護頁。</summary>
        /// <returns>輸出參數：IActionResult，Views/TableExcel/Index。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：頁面呼叫 backend TableExcel API（分頁、上傳 Excel、欄位對應）。
        /// 訊息內容及生成條件：驗證與系統錯誤訊息由後端 JsonValidFail/GetMsg 回傳。
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }
    }
}
