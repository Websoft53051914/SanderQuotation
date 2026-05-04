using Business.BusinessLogic;
using CommonClass.Models;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : BaseProjectController
    {
        public MenuController(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// 依登入帳號的角色權限，回傳 Sidebar 樹狀 MENU 資料
        /// </summary>
        [HttpPost("GetMAIN")]
        public IActionResult GetMAIN()
        {
            try
            {
                var bl = GetBLInstance<MenuBL>();
                var menuData = bl.GetMainMenuTree(UserInfo.UserAccount ?? string.Empty);

                return Ok(new DispatcherResponse_Menu_Shell
                {
                    Success = true,
                    Data    = menuData
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return Ok(new DispatcherResponse_Menu_Shell
                {
                    Success = false,
                    Data    = new DispatcherResponse_Menu { Data = new List<DispatcherData_Menu>(), ErrorMsg = ex.Message }
                });
            }
        }
    }
}
