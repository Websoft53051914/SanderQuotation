using frontend.Common.Attribute;
using Microsoft.AspNetCore.Mvc;
using static Const.Enums;

namespace frontend.Controllers.Permission
{
    public partial class PermissionController
    {
        public IActionResult Edit(Guid id)
        { 
            return View();
        }
    }
}
