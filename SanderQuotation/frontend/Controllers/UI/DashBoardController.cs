using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace frontend.Controllers
{
    [Route("UI/DashBoard/[action]")]
    public class DashBoardController : Controller
    {
        /// <summary>功能說明：顯示 Index 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }

        [ActionName("Analytics")]
        /// <summary>功能說明：顯示 Analytics 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Analytics()
        {
            return View();
        }

        [ActionName("CRM")]
        /// <summary>功能說明：顯示 CRM 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult CRM()
        {
            return View();
        }

        [ActionName("Crypto")]
        /// <summary>功能說明：顯示 Crypto 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Crypto()
        {
            return View();
        }

        [ActionName("Projects")]
        /// <summary>功能說明：顯示 Projects 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Projects()
        {
            return View();
        }

        [ActionName("NFT")]
        /// <summary>功能說明：顯示 NFT 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult NFT()
        {
            return View();
        }

        [ActionName("Job")]
        /// <summary>功能說明：顯示 Job 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Job()
        {
            return View();
        }
        public IActionResult Blog()
        {
            return View();
        }

    }
}

