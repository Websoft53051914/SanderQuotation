using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class SupportTicketsController : Controller
    {

        [ActionName("ListView")]
        /// <summary>功能說明：顯示 ListView 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult ListView()
        {
            return View();
        }

        [ActionName("TicketDetails")]
        /// <summary>功能說明：顯示 TicketDetails 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult TicketDetails()
        {
            return View();
        }

    }
}

