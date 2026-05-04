using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Const.Enums;

namespace frontend.Controllers.Dashboard
{
    public partial class DashboardController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public DashboardController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }
    }

    public partial class DashboardController
    {
        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult Create()
        {

            return View("Edit");
        }

        public IActionResult Edit()
        {

            return View("Edit");
        }
    }
}
