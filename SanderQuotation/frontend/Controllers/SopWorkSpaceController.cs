using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    /// <summary>
    /// SOP 工規工作站頁面 Controller
    /// </summary>
    public class SopWorkSpaceController : BaseProjectController
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// 建構子
        /// </summary>
        public SopWorkSpaceController(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>
        /// 工規瀏覽主頁
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }
    }
}
