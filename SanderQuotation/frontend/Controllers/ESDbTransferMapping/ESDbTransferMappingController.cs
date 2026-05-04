using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace frontend.Controllers.ESDbTransferMapping
{
    public partial class ESDbTransferMappingController : BaseProjectController
    {
        private readonly IConfiguration _config;

        public ESDbTransferMappingController(IConfiguration configuration)
        {
            _config = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

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
