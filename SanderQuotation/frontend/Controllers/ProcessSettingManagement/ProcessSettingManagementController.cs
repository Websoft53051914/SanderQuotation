using CommonClass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using static Const.Enums;

namespace frontend.Controllers.ProcessSettingManagement
{
    public partial class ProcessSettingManagementController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public ProcessSettingManagementController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }
    }
    public partial class ProcessSettingManagementController
    {
        public IActionResult Index()
        {
            List<SelectListItem> statusList = GetSelectListHandler().GetSelectListEnum<LogStatusEnum>();
            ViewData["StatusList"] = statusList;
            return View();
        }

        public IActionResult Create()
        {
            List<SelectListItem> statusList = GetSelectListHandler().GetSelectListEnum<StatusEnum>();
            ViewData["StatusList"] = statusList;
            return View("Edit", new BaseVM());
        }

        public IActionResult Edit(long id)
        {
            List<SelectListItem> statusList = GetSelectListHandler().GetSelectListEnum<StatusEnum>();
            ViewData["StatusList"] = statusList;
            return View(new BaseVM() { Id = id });
        }
    }
}
