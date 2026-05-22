using backend.AI;
using backend.AI.VO;
using backend.Common.Attribute;
using Microsoft.AspNetCore.Mvc;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/AIChat")]
    public class AIChatController : BaseProjectController
    {
        private readonly AIChatHandler _aiChatHandler;

        public AIChatController(IConfiguration config, AIChatHandler aiChatHandler) : base(config)
        {
            _aiChatHandler = aiChatHandler;
        }

        /// <summary>
        /// AI 對話：支援多輪追問，前端需將回傳的 SessionId 帶回下一輪
        /// </summary>
        [HttpPost("Chat")]
        [CustomAuthorization(FuncID.HistoryFile_View)]
        public async Task<IActionResult> Chat([FromBody] AIChatRequestVO request)
        {
            if (string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest(new { Success = false, Answer = "請輸入問題。" });

            var result = await _aiChatHandler.ChatAsync(request.SessionId, request.UserMessage);
            return Ok(result);
        }

        /// <summary>
        /// 清除指定 Session 的對話歷史（開啟新對話時呼叫）
        /// </summary>
        [HttpDelete("Session/{sessionId}")]
        [CustomAuthorization(FuncID.HistoryFile_View)]
        public IActionResult ClearSession(string sessionId)
        {
            _aiChatHandler.ClearSession(sessionId);
            return Ok(new { Success = true });
        }
    }
}
