using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    /// <summary>轉入檔案上傳 前端 Controller</summary>
    public class ImportTransferExcelController : BaseProjectController
    {
        /// <summary>轉入檔案上傳清單頁</summary>
        public IActionResult Index()
        {
            return View();
        }
    }
}
