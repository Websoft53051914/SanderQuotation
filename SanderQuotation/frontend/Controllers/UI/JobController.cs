using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class JobController : Controller
    {
        [ActionName("Statistics")]
        /// <summary>功能說明：顯示 Statistics 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Statistics()
        {
            return View();
        }

        [ActionName("Application")]
        /// <summary>功能說明：顯示 Application 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Application()
        {
            return View();
        }

        [ActionName("NewJob")]
        /// <summary>功能說明：顯示 NewJob 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult NewJob()
        {
            return View();
        }

        [ActionName("CompaniesList")]
        /// <summary>功能說明：顯示 CompaniesList 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult CompaniesList()
        {
            return View();
        }

        [ActionName("JobCategories")]
        /// <summary>功能說明：顯示 JobCategories 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult JobCategories()
        {
            return View();
        }

        [ActionName("List")]
        /// <summary>功能說明：顯示 List 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult List()
        {
            return View();
        }

        [ActionName("Grid")]
        /// <summary>功能說明：顯示 Grid 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Grid()
        {
            return View();
        }

        [ActionName("Overview")]
        /// <summary>功能說明：顯示 Overview 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Overview()
        {
            return View();
        }

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

        [ActionName("GridView")]
        /// <summary>功能說明：顯示 GridView 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult GridView()
        {
            return View();
        }

    }
}

