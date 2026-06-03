using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace frontend.Controllers.ESDbTransferMapping
{
    /// <summary>DB 轉檔對應設定前端 Controller。</summary>
    public partial class ESDbTransferMappingController : BaseProjectController
    {
        private readonly IConfiguration _config;

        /// <summary>功能說明：注入組態（BackendURL、JWT Cookie 名稱等）。</summary>
        /// <param name="configuration">輸入參數：IConfiguration。</param>
        /// <remarks>參考功能名稱與用途：BaseProjectController。訊息內容及生成條件：建構子無 HTTP 回應。</remarks>
        public ESDbTransferMappingController(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>功能說明：顯示 DB 轉檔對應清單頁。</summary>
        /// <returns>輸出參數：IActionResult，Views/ESDbTransferMapping/Index。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：頁面 AJAX 呼叫 api/ESDbTransferMapping。
        /// 訊息內容及生成條件：清單 CRUD 訊息由後端 API 回傳；本 Action 僅載入 View。
        /// </remarks>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>功能說明：伺服器端渲染單筆轉檔對應詳細頁（呼叫後端 Get API）。</summary>
        /// <param name="id">輸入參數：對應設定主鍵 Guid。</param>
        /// <returns>輸出參數：View("DetailView", ESDbTransferMappingVM)；失敗時 NotFound 或 StatusCode(500)。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：HttpClient POST api/ESDbTransferMapping/Get；Cookie jwtname 作為 Bearer；ApiResponse 反序列化。
        /// 訊息內容及生成條件：HTTP 非成功或 Data 為 null → NotFound()；例外 → LogError 後 500；成功 → DetailView。
        /// </remarks>
        public async Task<IActionResult> DetailView(Guid id)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["jwtname"]);

                var backendUrl = _config["BackendURL"];
                var response = await httpClient.PostAsync($"{backendUrl}/api/ESDbTransferMapping/Get?id={id}", null);

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<CommonClass.Model.ApiResponse>(jsonResponse,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data == null)
                    return NotFound();

                var vm = System.Text.Json.JsonSerializer.Deserialize<ESDbTransferMappingVM>(
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
