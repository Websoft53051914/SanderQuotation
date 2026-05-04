using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class AppsController : Controller
    {
        [ActionName("Calendar")]
        public IActionResult Calendar()
        {
            return View();
        }

        [ActionName("CalendarMonthGrid")]
        public IActionResult CalendarMonthGrid()
        {
            return View();
        }

        [ActionName("Chat")]
        public IActionResult Chat()
        {
            return View();
        }

        [ActionName("Mailbox")]
        public IActionResult Mailbox()
        {
            return View();
        }

        [ActionName("FileManager")]
        public IActionResult FileManager()
        {
            return View();
        }

        [ActionName("Todo")]
        public IActionResult Todo()
        {
            return View();
        }

        [ActionName("BasicAction")]
        public IActionResult BasicAction()
        {
            return View();
        }

        [ActionName("MailEcommerceAction")]
        public IActionResult MailEcommerceAction()
        {
            return View();
        }

        [ActionName("APIKey")]
        public IActionResult APIKey()
        {
            return View();
        }


    }
}
