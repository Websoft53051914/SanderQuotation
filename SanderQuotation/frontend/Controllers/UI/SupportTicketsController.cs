using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class SupportTicketsController : Controller
    {

        [ActionName("ListView")]
        public IActionResult ListView()
        {
            return View();
        }

        [ActionName("TicketDetails")]
        public IActionResult TicketDetails()
        {
            return View();
        }

    }
}
