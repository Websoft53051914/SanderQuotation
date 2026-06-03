using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    /// <summary>
    /// 轉入檔案上傳 前端 Controller
    /// </summary>
    public class EsFileTransferUploadController : BaseProjectController
    {
        /// <summary>功能說明：顯示 BOM/轉入檔案上傳與管理清單頁。</summary>
        /// <returns>輸出參數：IActionResult，Views/EsFileTransferUpload/Index。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：頁面透過 api/EsFileTransferUpload、Filepond 相關 API 操作資料。
        /// 訊息內容及生成條件：CRUD 訊息由後端 JsonSuccess/JsonValidFail 回傳；本 Action 僅載入 View。
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>功能說明：將舊路徑 /ImportTransferExcel 導向正式路由。</summary>
        /// <returns>輸出參數：RedirectToAction(Index)。</returns>
        [HttpGet("/ImportTransferExcel")]
        [HttpGet("/ImportTransferExcel/Index")]
        public IActionResult ImportTransferExcel()
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
