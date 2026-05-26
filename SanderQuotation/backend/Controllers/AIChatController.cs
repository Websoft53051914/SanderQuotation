using backend.AI;
using backend.AI.VO;
using backend.Common;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using Google.GenAI;
using Microsoft.AspNetCore.Mvc;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/AIChat")]
    public class AIChatController : BaseProjectController
    {
        private readonly AIChatHandler _aiChatHandler;
        private readonly PathProvider _pathProvider;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="aiChatHandler"></param>
        /// <param name="pathProvider"></param>
        public AIChatController(IConfiguration config
            , AIChatHandler aiChatHandler
            , PathProvider pathProvider) : base(config)
        {
            _aiChatHandler = aiChatHandler;
            _pathProvider = pathProvider;
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


            try
            {
                var aiLogBL = GetBLInstance<AILogBL>();
                aiLogBL.Insert(new AILogDM
                {
                    Role = (int)AILogRoleEnum.User,
                    Content = request.UserMessage
                });

                var result = await _aiChatHandler.ChatAsync(request.SessionId, request.UserMessage);

                aiLogBL.Insert(new AILogDM
                {
                    Role = (int)AILogRoleEnum.Assistant,
                    Content = result.Answer,
                    Sql = result.GeneratedSql,
                    Function = result.FunctionName
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not shown here)
                LogError(ex);
                return StatusCode(500, new { Success = false, Answer = "AI 回答失敗，請稍後再試。" });
            }
        }

        /// <summary>
        /// 清除指定 Session 的對話歷史（開啟新對話時呼叫）
        /// </summary>
        [HttpDelete("Session/{sessionId}")]
        [CustomAuthorization(FuncID.HistoryFile_View)]
        public IActionResult ClearSession(string sessionId)
        {
            try
            {
                _aiChatHandler.ClearSession(sessionId);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                // Log the exception (not shown here)
                LogError(ex);
                return StatusCode(500, new { Success = false, Message = "清除對話歷史失敗，請稍後再試。" });
            }
            
        }

        /// <summary>
        /// 下載 AI 產生檔案
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpGet("DownloadAIFile")]
        public IActionResult DownloadAIFile(string fileName)
        {
            try
            {
                string dirPath = _pathProvider.AIExcel;
                string filePath = dirPath + "/" + fileName;
                if (!System.IO.File.Exists(filePath))
                {
                    return BadRequest(JsonValidFail("檔案不存在"));
                }
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, MimeTypes.GetMimeType(fileName), fileName);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return BadRequest(JsonValidFail(_config[$"message:{LoginSession.Current.Locale}:SystemErrorMsg"]));
            }
        }
    }
}
