using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace frontend.Controllers
{
    [Route("UI/DashBoard/[action]")]
    public class DashBoardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [ActionName("Analytics")]
        public IActionResult Analytics()
        {
            return View();
        }

        [ActionName("CRM")]
        public IActionResult CRM()
        {
            return View();
        }

        [ActionName("Crypto")]
        public IActionResult Crypto()
        {
            return View();
        }

        [ActionName("Projects")]
        public IActionResult Projects()
        {
            return View();
        }

        [ActionName("NFT")]
        public IActionResult NFT()
        {
            return View();
        }

        [ActionName("Job")]
        public IActionResult Job()
        {
            return View();
        }
        public IActionResult Blog()
        {
            return View();
        }

    }
}
