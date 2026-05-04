using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers.FoupLot
{
    public class FoupLotController : BaseProjectController
    {
        private readonly IConfiguration _config;

        public FoupLotController(IConfiguration configuration)
        {
            _config = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SplitLot(string id)
        {
            ViewBag.LotId = id ?? "LOT-DEMO-001";
            return View();
        }

        public IActionResult MergeLot(string id)
        {
            ViewBag.LotId = id ?? "LOT-DEMO-001";
            return View();
        }
    }
}
