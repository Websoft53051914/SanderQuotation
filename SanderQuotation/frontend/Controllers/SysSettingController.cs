using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class SysSettingController : BaseProjectController
    {
        private readonly IConfiguration _config;
        public SysSettingController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
