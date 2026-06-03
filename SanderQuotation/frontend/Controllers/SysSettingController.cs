using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers
{
    /// <summary>系統參數設定前端 Controller。</summary>
    public class SysSettingController : BaseProjectController
    {
        private readonly IConfiguration _config;

        /// <summary>功能說明：注入應用程式組態。</summary>
        /// <param name="config">輸入參數：IConfiguration。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public SysSettingController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>功能說明：顯示系統參數設定頁（AI 開關、偏好廠商、品牌比對類別等）。</summary>
        /// <returns>輸出參數：IActionResult，Views/SysSetting/Index。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：頁面呼叫 api/SysSetting（GetData、Create、Edit、Delete、SaveOther）。
        /// 訊息內容及生成條件：「新增成功」「編輯成功」等由後端 JsonSuccess 回傳；例外 → System_Error。
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }
    }
}
