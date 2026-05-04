using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers.TableExcel
{
    public class TableExcelController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public TableExcelController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _config = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
