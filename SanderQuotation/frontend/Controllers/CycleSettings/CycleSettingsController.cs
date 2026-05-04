using frontend.Common;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers.CycleSettings
{
    public partial class CycleSettingsController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public CycleSettingsController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
