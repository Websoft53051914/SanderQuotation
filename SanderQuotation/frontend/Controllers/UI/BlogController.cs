using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace frontend.Controllers
{
    public class BlogController : Controller
    {
        // GET: Blog
        public IActionResult grid()
        {
            return View();
        }
        public IActionResult list()
        {
            return View();
        }
        public IActionResult overview()
        {
            return View();
        }
    }
}