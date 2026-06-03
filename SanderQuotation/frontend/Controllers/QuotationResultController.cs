using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    /// <summary>
    /// 定時查價結果 前端 Controller
    /// </summary>
    public class QuotationResultController : BaseProjectController
    {
        /// <summary>功能說明：顯示定時查價結果清單頁，由前端呼叫 backend api/QuotationResult。</summary>
        /// <returns>輸出參數：IActionResult，Views/QuotationResult/Index。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：Razor 內 AJAX → QuotationResult/GetPageList。
        /// 訊息內容及生成條件：頁面內 JsonSuccess/JsonValidFail 由後端 API 回傳，本 Action 僅載入殼層。
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>功能說明：顯示單筆查價結果詳細/編輯頁。</summary>
        /// <param name="Id">輸入參數：查價檔案主鍵 Guid，傳入 ViewBag.QuotationFileId。</param>
        /// <returns>輸出參數：IActionResult，Views/QuotationResult/Edit。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：ViewBag.QuotationFileId；頁面呼叫 GetById、ReInternalQuotation 等 API。
        /// 訊息內容及生成條件：資料與錯誤訊息由後端 API 回傳；本 Action 僅設定 Id 並渲染 View。
        /// </remarks>
        public IActionResult Edit(Guid Id)
        {
            ViewBag.QuotationFileId = Id;
            return View("Edit");
        }
    }
}
