using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    /// <summary>
    /// 定時查價結果 前端 Controller
    /// </summary>
    public class QuotationResultController : BaseProjectController
    {
        /// <summary>
        /// 查價結果清單頁
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查價結果編輯頁
        /// </summary>
        public IActionResult Edit(Guid Id)
        {
            ViewBag.QuotationFileId = Id;
            return View("Edit");
        }
    }
}
