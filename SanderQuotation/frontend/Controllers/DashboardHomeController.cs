using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class DashboardHomeController : BaseProjectController
    {
        private readonly ILogger<HomeController> _logger;

        public DashboardHomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
