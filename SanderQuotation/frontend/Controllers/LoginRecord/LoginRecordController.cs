using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using frontend.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using static Const.Enums;

namespace frontend.Controllers.LoginRecord
{
    public partial class LoginRecordController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public LoginRecordController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }
    }
    public partial class LoginRecordController
    {
        public IActionResult Index()
        {
            List<SelectListItem> statusList = GetSelectListHandler().GetSelectListEnum<LogStatusEnum>();
            ViewData["StatusList"] = statusList;
            return View();
        }
    }
}
