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

        public TestExternalAPIController(IConfiguration config, ExternalQueryExecuteHandler externalQueryExecuteHandler) : base(config)
        {
            _externalQueryExecuteHandler = externalQueryExecuteHandler;
        }

        /// <summary>
        /// 測試 Mouser 查詢（搜尋料號 + 購物車取得實際下單價）
        /// </summary>
        /// <param name="req">料號與目標數量</param>
        [HttpPost("QueryMouser")]
        [AllowAnonymous]
        public async Task<IActionResult> QueryMouser([FromBody] QueryMouserCartPriceReqVO req)
        {
            var results = await _externalQueryExecuteHandler.QueryMouserAction(req);
            return Ok(results);
        }

        /// <summary>
        /// 測試 DigiKey 查詢（含 MyPricing 優惠價）
        /// </summary>
        /// <param name="req">料號與目標數量</param>
        [HttpPost("QueryDk")]
        [AllowAnonymous]
        public async Task<IActionResult> QueryDk([FromBody] QueryMouserCartPriceReqVO req)
        {
            var results = await _externalQueryExecuteHandler.QueryDkAction(req);
            return Ok(results);
        }
    }
}
