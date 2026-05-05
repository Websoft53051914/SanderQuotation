using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers.SourceDashboard
{
    public class SourceDashboardController : BaseProjectController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
