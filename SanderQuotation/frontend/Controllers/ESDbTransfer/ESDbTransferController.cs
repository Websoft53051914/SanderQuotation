using Microsoft.AspNetCore.Mvc;
using ViewModel;
using Sander.Platform.DbTransfer;
using static Const.Enums;

namespace frontend.Controllers.ESDbTransfer
{
    /// <summary>DB 連線轉檔設定前端 Controller。</summary>
    public partial class ESDbTransferController : BaseProjectController
    {
        private readonly IConfiguration _config;

        /// <summary>功能說明：注入應用程式組態。</summary>
        /// <param name="configuration">輸入參數：IConfiguration。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public ESDbTransferController(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>功能說明：顯示 DB 轉檔連線清單頁，並載入資料庫類型下拉選項。</summary>
        /// <returns>輸出參數：IActionResult，Views/ESDbTransfer/Index；ViewData 含 DbTypeSelectList。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetSelectListHandler().GetSelectListEnum&lt;EsDbTransferDbTypeEnum&gt;。
        /// 訊息內容及生成條件：維護訊息由後端 api/ESDbTransfer 回傳。
        /// </remarks>
        public IActionResult Index()
        {
            ViewData["DbTypeSelectList"] = GetSelectListHandler().GetSelectListEnum<EsDbTransferDbTypeEnum>();
            return View();
        }

        /// <summary>功能說明：伺服器端渲染單筆 DB 轉檔連線詳細頁。</summary>
        /// <param name="id">輸入參數：連線設定主鍵 Guid。</param>
        /// <returns>輸出參數：View("DetailView", ESDbTransferVM)；失敗時 NotFound 或 500。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：HttpClient POST api/ESDbTransfer/Get；Bearer jwtname Cookie。
        /// 訊息內容及生成條件：API 失敗或無 Data → NotFound；例外 → LogError + StatusCode(500)。
        /// </remarks>
        public async Task<IActionResult> DetailView(Guid id)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["jwtname"]);

                var backendUrl = _config["BackendURL"];
                var response = await httpClient.PostAsync($"{backendUrl}/api/ESDbTransfer/Get?id={id}", null);

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<CommonClass.Model.ApiResponse>(jsonResponse,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data == null)
                    return NotFound();

                var vm = System.Text.Json.JsonSerializer.Deserialize<ESDbTransferVM>(
                    result.Data.ToString(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return View("DetailView", vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return StatusCode(500);
            }
        }
    }
}
