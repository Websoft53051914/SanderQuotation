using backend.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static backend.Common.ExternalQueryExecuteHandler;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/TestExternalAPI")]
    public class TestExternalAPIController : BaseProjectController
    {
        private readonly ExternalQueryExecuteHandler _externalQueryExecuteHandler;

        /// <summary>
        /// 功能說明：注入外部查價執行器，供測試 API 使用。
        /// </summary>
        /// <param name="config">輸入參數：應用程式組態（傳入 BaseProjectController）。</param>
        /// <param name="externalQueryExecuteHandler">輸入參數：Mouser/DigiKey 查價處理器實例。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController — 基底授權與組態存取。
        /// 訊息內容及生成條件：建構子本身不產生 API 回應。
        /// </remarks>
        public TestExternalAPIController(IConfiguration config, ExternalQueryExecuteHandler externalQueryExecuteHandler) : base(config)
        {
            _externalQueryExecuteHandler = externalQueryExecuteHandler;
        }

        /// <summary>
        /// 功能說明：測試 Mouser 查詢（搜尋料號並以購物車取得實際下單價）。
        /// </summary>
        /// <param name="req">輸入參數：料號（PartNumber）與目標數量（Quantity）。</param>
        /// <returns>輸出參數：OkObjectResult，內容為 QueryActionResultRspVO（含 Result 與 DecisionLogs）。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：ExternalQueryExecuteHandler.QueryMouserAction — 呼叫 Mouser API 並組裝查價結果與決策歷程。
        /// 訊息內容及生成條件：直接回傳 Handler 結果；Handler 內 API 失敗時 DecisionLogs 含錯誤訊息，Result 可能為 null。
        /// </remarks>
        [HttpPost("QueryMouser")]
        [AllowAnonymous]
        public async Task<IActionResult> QueryMouser([FromBody] QueryMouserCartPriceReqVO req)
        {
            var results = await _externalQueryExecuteHandler.QueryMouserAction(req);
            return Ok(results);
        }

        /// <summary>
        /// 功能說明：測試 DigiKey 查詢（含 MyPricing 優惠價）。
        /// </summary>
        /// <param name="req">輸入參數：料號（PartNumber）與目標數量（Quantity）。</param>
        /// <returns>輸出參數：OkObjectResult，內容為 QueryActionResultRspVO（含 Result 與 DecisionLogs）。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：ExternalQueryExecuteHandler.QueryDkAction — 呼叫 DigiKey API 並組裝查價結果與決策歷程。
        /// 訊息內容及生成條件：直接回傳 Handler 結果；Handler 內 API 失敗時 DecisionLogs 含錯誤訊息，Result 可能為 null。
        /// </remarks>
        [HttpPost("QueryDk")]
        [AllowAnonymous]
        public async Task<IActionResult> QueryDk([FromBody] QueryMouserCartPriceReqVO req)
        {
            var results = await _externalQueryExecuteHandler.QueryDkAction(req);
            return Ok(results);
        }
    }
}
