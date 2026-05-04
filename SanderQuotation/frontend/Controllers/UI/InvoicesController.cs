using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class InvoicesController : Controller
    {

        [ActionName("ListView")]
        public IActionResult ListView()
        {
            return View();
        }

        [ActionName("Details")]
        public IActionResult Details()
        {
            return View();
        }

        [ActionName("CreateInvoice")]
        public IActionResult CreateInvoice()
        {
            return View();
        }

    }
}
