using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace frontend.Controllers
{
    public class HistoryFileController : BaseProjectController
    {
        private readonly IConfiguration _config;

        public HistoryFileController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
