using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class OcapDesignController : BaseProjectController
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SaveFlow([FromBody] object flowJson)
        {
            // Persist to DB / file as needed
            return Json(new { success = true });
        }
    }
}
