using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace frontend.Controllers
{
    public class LockController : BaseProjectController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LockController(IHttpClientFactory factory)
        {
            _httpClientFactory = factory;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Lock()
        {
            var client = _httpClientFactory.CreateClient("api");
            var token = Request.Cookies["jwtname"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync("api/system-lock/lock", null);
            HttpContext.Session.SetString("SystemLocked", "true");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Unlock([FromBody] UnlockDto dto)
        {
            var client = _httpClientFactory.CreateClient("api");
            var token = Request.Cookies["jwtname"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var res = await client.PostAsJsonAsync("api/system-lock/unlock", new { Pincode = dto.Pincode } // 注意大小寫對應 DTO
            );

            if (!res.IsSuccessStatusCode)
            {
                return JsonValidFail("PIN 錯誤");
                //ViewBag.Error = "PIN 錯誤";
                //return View("Index");
            }

            HttpContext.Session.Remove("SystemLocked");
            return JsonSuccess("");
            HttpContext.Session.Remove("SystemLocked");
            return RedirectToAction("Index", "Home");
        }

        public class UnlockDto
        {
            [JsonPropertyName("pincode")]
            public string Pincode { get; set; }
        }
    }

}
