using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class RecipeMgmtController : BaseProjectController
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Edit(long Id)
        {
            ViewBag.RecipeId = Id;
            return View("Edit");
        }
    }
}
