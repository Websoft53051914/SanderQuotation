using Core.Utility.Utility;
using frontend.Common;
using Microsoft.AspNetCore.Mvc;
using static Const.Enums;

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
            ViewData["OtherTransferSettingSelectList"] = GetSelectListHandler().GetSelectListEnum<ScheduleCycleActionTypeEnum>();
            return View();
        }
    }
}
