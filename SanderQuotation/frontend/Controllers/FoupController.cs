using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class FoupController : BaseProjectController
    {
        public IActionResult Index(string lotId = null, string location = null)
        {
            ViewBag.LotId = lotId;
            ViewBag.Location = location;
            return View();
        }
    }
}
