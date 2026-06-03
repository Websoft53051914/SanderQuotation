using frontend.Common.Attribute;
using Microsoft.AspNetCore.Mvc;
using static Const.Enums;

namespace frontend.Controllers.Permission
{
    public partial class PermissionController
    {
        /// <summary>功能說明：顯示編輯帳號權限頁。</summary>
        /// <param name="id">輸入參數：帳號或權限資料主鍵 Guid。</param>
        /// <returns>輸出參數：IActionResult，Views/Permission/Edit。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：頁面依 id 呼叫後端 API 載入資料。
        /// 訊息內容及生成條件：載入與儲存訊息由 API JsonSuccess/JsonValidFail 回傳至前端顯示。
        /// </remarks>
        public IActionResult Edit(Guid id)
        {
            return View();
        }
    }
}
