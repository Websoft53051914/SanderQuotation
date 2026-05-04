using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    /// <summary>
    /// 版號規則設定頁面 Controller
    /// </summary>
    public class RecipeVersionNoRuleController : BaseProjectController
    {
        /// <summary>版號規則設定清單頁</summary>
        public IActionResult Index() => View();
    }
}
