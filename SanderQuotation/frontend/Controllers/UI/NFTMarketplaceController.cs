using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    public class NFTmarketplaceController : Controller
    {

        [ActionName("Marketplace")]
        public IActionResult Marketplace()
        {
            return View();
        }

        [ActionName("ExploreNow")]
        public IActionResult ExploreNow()
        {
            return View();
        }

        [ActionName("LiveAuction")]
        public IActionResult LiveAuction()
        {
            return View();
        }

        [ActionName("ItemDetails")]
        public IActionResult ItemDetails()
        {
            return View();
        }

        [ActionName("Collections")]
        public IActionResult Collections()
        {
            return View();
        }

        [ActionName("Creators")]
        public IActionResult Creators()
        {
            return View();
        }

        [ActionName("Ranking")]
        public IActionResult Ranking()
        {
            return View();
        }

        [ActionName("WalletConnect")]
        public IActionResult WalletConnect()
        {
            return View();
        }

        [ActionName("CreateNFT")]
        public IActionResult CreateNFT()
        {
            return View();
        }


    }
}
