using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

namespace frontend.Controllers
{
    public class SysRoleController : BaseProjectController
    {
        private readonly IConfiguration _config;

        public SysRoleController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 系統角色管理首頁
        /// </summary>
        public IActionResult Index()
        {
            var vm = new SysRoleVM();
            ViewData["StatusSelectList"] = GetSelectListHandler().GetSelectListEnum<StatusEnum>().Where(x => x.Value != ((int)StatusEnum.Cancel).ToString()).OrderByDescending(x => x.Value).ToList();
            return View(vm);
        }

        /// <summary>
        /// 權限設定頁面
        /// </summary>
        public IActionResult Permission(Guid id)
        {
            var vm = new SysRoleVM
            {
                Id = id
            };
            return View(vm);
        }
    }
}
