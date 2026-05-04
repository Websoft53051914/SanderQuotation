using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class WidgetsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
