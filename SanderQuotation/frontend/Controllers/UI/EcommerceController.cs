using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class EcommerceController : Controller
    {

        [ActionName("Products")]
        public IActionResult Products()
        {
            return View();
        }


        [Route("apps-ecommerce-product-details.html")]
        public IActionResult ProductDetails()
        {
            return View();
        }

        [ActionName("CreateProduct")]
        public IActionResult CreateProduct()
        {
            return View();
        }

        [ActionName("Orders")]
        public IActionResult Orders()
        {
            return View();
        }

        [ActionName("OrderDetails")]
        [Route("Ecommerce/apps-ecommerce-order-details.html")]
        public IActionResult OrderDetails()
        {
            return View();
        }

        [ActionName("Customers")]
        public IActionResult Customers()
        {
            return View();
        }

        [ActionName("ShoppingCart")]
        public IActionResult ShoppingCart()
        {
            return View();
        }

        [ActionName("Checkout")]
        public IActionResult Checkout()
        {
            return View();
        }

        [ActionName("Sellers")]
        public IActionResult Sellers()
        {
            return View();
        }

        [ActionName("SellerDetails")]
        public IActionResult SellerDetails()
        {
            return View();
        }

    }
}
