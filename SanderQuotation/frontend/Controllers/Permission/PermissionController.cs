using CommonClass.Models;
using frontend.Common;
using frontend.Common.Attribute;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using System.Web;
using static Const.Enums;

namespace frontend.Controllers.Permission
{
    public partial class PermissionController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public PermissionController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        //[CustomAuthorization(FuncID.Permission_View)]
        public IActionResult Index()
        {
            var accountStatusList = Method.GetAccountStatusForCreate();
            accountStatusList.AddRange(Method.GetStatus());
            accountStatusList = accountStatusList.OrderBy(s => s.Value).ToList();
            accountStatusList.Insert(0, new SelectListItem() { Text = "全部", Value = " " });
            ViewData["accountStatusList"] = accountStatusList.OrderBy(s => s.Value).ToList();
            WebMethod.SetFuncIdAndClassName(ViewData, HttpContext.Request);

            return View();
        }
    }
}
