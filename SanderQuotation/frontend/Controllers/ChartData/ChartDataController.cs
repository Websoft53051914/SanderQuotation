using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers.ChartData
{
    public partial class ChartDataController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public ChartDataController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }
    }

    public partial class ChartDataController
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
