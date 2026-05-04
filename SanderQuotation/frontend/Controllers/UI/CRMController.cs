using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class CRMController : Controller
    {

        [ActionName("Contacts")]
        public IActionResult Contacts()
        {
            return View();
        }

        [ActionName("Companies")]
        public IActionResult Companies()
        {
            return View();
        }

        [ActionName("Deals")]
        public IActionResult Deals()
        {
            return View();
        }

        [ActionName("Leads")]
        public IActionResult Leads()
        {
            return View();
        }

    }
}
