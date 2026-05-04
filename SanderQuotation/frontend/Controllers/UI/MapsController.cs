using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class MapsController : Controller
    {

        [ActionName("Google")]
        public IActionResult Google()
        {
            return View();
        }

        [ActionName("Vector")]
        public IActionResult Vector()
        {
            return View();
        }

        [ActionName("Leaflet")]
        public IActionResult Leaflet()
        {
            return View();
        }

    }
}
