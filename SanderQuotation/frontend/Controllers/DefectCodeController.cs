using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class DefectCodeController : BaseProjectController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
