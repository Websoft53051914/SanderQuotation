using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class EcommerceController : Controller
    {

        [ActionName("Products")]
        /// <summary>功能說明：顯示 Products 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Products()
        {
            return View();
        }


        [Route("apps-ecommerce-product-details.html")]
        /// <summary>功能說明：顯示 ProductDetails 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult ProductDetails()
        {
            return View();
        }

        [ActionName("CreateProduct")]
        /// <summary>功能說明：顯示 CreateProduct 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult CreateProduct()
        {
            return View();
        }

        [ActionName("Orders")]
        /// <summary>功能說明：顯示 Orders 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Orders()
        {
            return View();
        }

        [ActionName("OrderDetails")]
        [Route("Ecommerce/apps-ecommerce-order-details.html")]
        /// <summary>功能說明：顯示 OrderDetails 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult OrderDetails()
        {
            return View();
        }

        [ActionName("Customers")]
        /// <summary>功能說明：顯示 Customers 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Customers()
        {
            return View();
        }

        [ActionName("ShoppingCart")]
        /// <summary>功能說明：顯示 ShoppingCart 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult ShoppingCart()
        {
            return View();
        }

        [ActionName("Checkout")]
        /// <summary>功能說明：顯示 Checkout 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Checkout()
        {
            return View();
        }

        [ActionName("Sellers")]
        /// <summary>功能說明：顯示 Sellers 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult Sellers()
        {
            return View();
        }

        [ActionName("SellerDetails")]
        /// <summary>功能說明：顯示 SellerDetails 頁面（Velzon UI 範本）。</summary>
        /// <returns>輸出參數：IActionResult，渲染對應 Razor View。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：無後端 API/BL，僅 MVC 檢視路由。
        /// 訊息內容及生成條件：Controller 不產生 JSON；頁面訊息由 Razor/前端處理。
        /// </remarks>
        public IActionResult SellerDetails()
        {
            return View();
        }

    }
}

