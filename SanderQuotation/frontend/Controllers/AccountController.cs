using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace frontend.Controllers
{
    public class AccountController : BaseProjectController
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 帳號管理首頁
        /// </summary>
        public IActionResult Index()
        {
            var vm = new AccountVM();
            return View(vm);
        }

        /// <summary>
        /// 帳號詳細頁面
        /// </summary>
        public IActionResult DetailView(Guid id)
        {
            var vm = new AccountVM
            {
                Id = id
            };
            return View(vm);
        }
    }
}
