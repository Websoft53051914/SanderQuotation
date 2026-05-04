using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class TablesController : Controller
    {
        [ActionName("BasicTables")]
        public IActionResult BasicTables()
        {
            return View();
        }

        [ActionName("GridJs")]
        public IActionResult GridJs()
        {
            return View();
        }

        [ActionName("ListJs")]
        public IActionResult ListJs()
        {
            return View();
        }
        [ActionName("Datatables")]
        public IActionResult Datatables()
        {
            return View();
        }
    }
}
