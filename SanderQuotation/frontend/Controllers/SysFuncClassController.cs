using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace frontend.Controllers
{
    public class SysFuncClassController : BaseProjectController
    {
        private readonly IConfiguration _config;

        public SysFuncClassController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 系統功能類別管理首頁
        /// </summary>
        public IActionResult Index()
        {
            var vm = new SysFuncClassVM();
            return View(vm);
        }
    }
}
