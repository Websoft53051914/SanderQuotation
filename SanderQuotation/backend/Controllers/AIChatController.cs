using backend.AI;
using backend.AI.VO;
using backend.Common;
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
        /// 功能說明：建立 AI 對話 Controller，注入對話處理器與檔案路徑提供者。
        /// </summary>
        /// <param name="config">輸入參數：應用程式組態。</param>
        /// <param name="aiChatHandler">輸入參數：AI 多輪對話與 SQL 產生處理器。</param>
        /// <param name="pathProvider">輸入參數：AI 產出檔案（如 Excel）的實體路徑提供者。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController — 基底授權與 LogError。
        /// 訊息內容及生成條件：建構子本身不產生 API 回應。
        /// </remarks>
        public AIChatController(IConfiguration config
            , AIChatHandler aiChatHandler
            , PathProvider pathProvider) : base(config)
        {
            _aiChatHandler = aiChatHandler;
            _pathProvider = pathProvider;
        }

        /// <summary>
        /// 功能說明：AI 多輪對話；前端需將回傳的 SessionId 帶入下一輪請求。
        /// </summary>
        /// <param name="request">輸入參數：SessionId、UserMessage（使用者問題）。</param>
        /// <returns>輸出參數：Ok(AIChatResponse) 含 Answer、SessionId、GeneratedSql 等；或 BadRequest/StatusCode(500)。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：AIChatHandler.ChatAsync — 執行對話與工具呼叫；AILogBL.Insert — 記錄使用者與助理訊息；GetBLInstance — 取得 BL。
        /// 訊息內容及生成條件：UserMessage 空白 → BadRequest「請輸入問題。」；成功 → Ok(result)；例外 → LogError 後 StatusCode(500)「AI 回答失敗，請稍後再試。」
        /// </remarks>
        [HttpPost("Chat")]
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
        /// 功能說明：清除指定 Session 的對話歷史（開啟新對話時呼叫）。
        /// </summary>
        /// <param name="sessionId">輸入參數：要清除的對話工作階段識別碼。</param>
        /// <returns>輸出參數：Ok({ Success = true })；失敗時 StatusCode(500)。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：AIChatHandler.ClearSession — 移除記憶體中的對話上下文。
        /// 訊息內容及生成條件：成功 → Ok Success=true；例外 → LogError 後 500「清除對話歷史失敗，請稍後再試。」
        /// </remarks>
        [HttpDelete("Session/{sessionId}")]
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
        /// 功能說明：下載 AI 產生的檔案（如 Excel），檔案位於 AIExcel 目錄。
        /// </summary>
        /// <param name="fileName">輸入參數：檔案名稱（含副檔名）。</param>
        /// <returns>輸出參數：FileStreamResult（檔案下載）；或 BadRequest(JsonValidFail)。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：PathProvider.AIExcel — 取得儲存目錄；MimeTypes.GetMimeType — 決定 Content-Type；JsonValidFail — 錯誤 JSON。
        /// 訊息內容及生成條件：檔案不存在 → BadRequest「檔案不存在」；成功 → File 串流下載；例外 → LogError 後 JsonValidFail(SystemErrorMsg，依語系 message 設定)。
        /// </remarks>
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
