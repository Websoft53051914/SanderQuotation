using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace frontend.Controllers
{
    public class SysFuncController : BaseProjectController
    {
        private readonly IConfiguration _config;

        public SysFuncController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 系統功能管理首頁
        /// </summary>
        public IActionResult Index()
        {
            var vm = new SysFuncVM();
            return View(vm);
        }
    }
}
