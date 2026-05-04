using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class ProjectsController : Controller
    {

        [ActionName("List")]
        public IActionResult List()
        {
            return View();
        }

        [ActionName("Overview")]
        public IActionResult Overview()
        {
            return View();
        }

        [ActionName("CreateProject")]
        public IActionResult CreateProject()
        {
            return View();
        }

    }
}
