using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class TasksController : Controller
    {

        [ActionName("KanbanBoard")]
        public IActionResult KanbanBoard()
        {
            return View();
        }

        [ActionName("ListView")]
        public IActionResult ListView()
        {
            return View();
        }

        [ActionName("TaskDetails")]
        public IActionResult TaskDetails()
        {
            return View();
        }

    }
}
