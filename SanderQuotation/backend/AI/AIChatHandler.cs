using backend.AI.VO;
using backend.AI.Plugins;
using backend.AI.VO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.Extensions.Resilience;
using Polly;
using Polly.Registry;
using System.Text;
using System.Text.Json;
using static Const.Enums;
using Core.Utility.Extensions;
using backend.Common;

namespace backend.AI
{
    /// <summary>
    /// AI 對話中心：自然語言 → Kernel Function Calling → 執行 → 自然語言回答（支援多輪追問）
    /// </summary>
    public class AIChatHandler
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _config;
        private readonly ManualBatchEmbedding _embedding;
        private readonly IMemoryCache _cache;
        private readonly ResiliencePipeline _resiliencePipeline;
        private readonly HistoryFileQueryPlugin _historyFileQueryPlugin;
        private readonly QuotationQueryPlugin _quotationQueryPlugin;

        // AI 回覆無法回答時的識別標記
        private const string CannotAnswerTag = "[CANNOT_ANSWER]";

        // Session Cache 保留時間（分鐘）
        private const int SessionCacheMinutes = 30;

        // ChatHistoryTruncationReducer 設定：
        //   targetCount  = 保留最近幾則 User/Assistant 訊息（10 則 = 5 輪對話）
        //   thresholdCount = 超過 targetCount 幾則才觸發壓縮（設 2 → 第 13 則時才壓）
        private static readonly ChatHistoryTruncationReducer _historyReducer =
            new(targetCount: 10, thresholdCount: 2);

        /// <summary>constructor</summary>
        public AIChatHandler(Kernel kernel, IConfiguration config, ManualBatchEmbedding embedding, IMemoryCache cache
            , HistoryFileQueryPlugin historyFileQueryPlugin
            , QuotationQueryPlugin quotationQueryPlugin
            , ReportPlugin reportPlugin
            , ResiliencePipelineProvider<string> resiliencePipelineProvider)
        {
            _kernel = kernel;
            _config = config;
            _embedding = embedding;
            _cache = cache;
            _resiliencePipeline = resiliencePipelineProvider.GetPipeline("ai-retry");
            _historyFileQueryPlugin = historyFileQueryPlugin;
            _quotationQueryPlugin = quotationQueryPlugin;

            // 將 Plugin 註冊進 Kernel，讓 Gemini Function Calling 能看到可用工具
            _kernel.Plugins.AddFromObject(historyFileQueryPlugin);
            _kernel.Plugins.AddFromObject(quotationQueryPlugin);
            _kernel.Plugins.AddFromObject(reportPlugin);
        }

        // ─────────────────────────────────────────────
        // System Prompt：Orchestrator，指引 AI 決定呼叫哪個 Function
        // ─────────────────────────────────────────────
        private string GetOrchestratorSystemPrompt()
        {
            return $@"
你是一個智慧資料查詢助理，可以透過工具（Function）查詢系統內的資料，並以自然語言回答使用者。

# 行為規則
1. 若使用者的問題與上述工具能查詢的資料有關，請呼叫對應的工具取得資料，再以繁體中文友善地回答。
2. 若問題與任何工具的查詢範圍完全無關（例如：問天氣、新聞、閒聊），請直接回覆：{CannotAnswerTag}
3. 支援多輪追問：當使用者說「上面」、「剛才」、「再篩選」、「它」等指代詞時，請參考對話歷史理解語意。
4. 取得工具回傳的 JSON 資料後，請用繁體中文以自然語氣摘要回答，不要直接把 JSON 丟給使用者。
5. 現在時間：{DateTime.Now:yyyy/MM/dd HH:mm:ss} {Method.GetDayName(DateTime.Now.DayOfWeek)}，本週範圍：{Method.GetWeekStart(DateTime.Now):yyyy/MM/dd}（週一）～ {Method.GetWeekEnd(DateTime.Now):yyyy/MM/dd}（週日），「本週」或「這週」一律指此範圍。

# 回答風格
- 簡潔、友善、使用繁體中文
- 有資料時：說明筆數並列舉重點
- 無資料時：明確告知查無資料
";
        }

        // ─────────────────────────────────────────────
        // 主入口：自然語言 → Function Calling → 自然語言
        // ─────────────────────────────────────────────
        /// <summary>
        /// 接收使用者問題，透過 Kernel Function Calling 自動選擇工具查詢，最後以自然語言回答
        /// </summary>
        public async Task<AIChatResponseVO> ChatAsync(string? sessionId, string userMessage)
        {
            string resolvedSessionId = string.IsNullOrWhiteSpace(sessionId)
                ? Guid.NewGuid().ToString("N")
                : sessionId;

            ChatHistory chatHistory = GetSessionChatHistory(resolvedSessionId);
            var response = new AIChatResponseVO { SessionId = resolvedSessionId };

            try
            {
                // 加入本輪使用者訊息（先放入 chatHistory，讓 AI 知道上下文）
                chatHistory.AddUserMessage(userMessage);

                // 設定 Function Calling 為自動模式
#pragma warning disable SKEXP0070
                var executionSettings = new GeminiPromptExecutionSettings
                {
                    ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
                };
#pragma warning restore SKEXP0070

                var chatService = _kernel.GetRequiredService<IChatCompletionService>();

                // 讓 Kernel 自動決定要不要呼叫 Function，並處理多輪 Function Call loop
                // 透過 Polly Resilience Pipeline 包覆，自動重試 + Timeout
                var result = await _resiliencePipeline.ExecuteAsync(async ct =>
                    await chatService.GetChatMessageContentAsync(
                        chatHistory,
                        executionSettings,
                        _kernel,
                        ct
                    )
                );

                string assistantReply = result.Content ?? string.Empty;

                // 偵測 AI 是否判斷問題超出範圍
                if (assistantReply.Contains(CannotAnswerTag))
                    throw new AIChatOutOfScopeException();

                response.Answer = assistantReply;
                response.Success = true;

                // 從 chatHistory 的 Function Result 訊息中，嘗試萃取 SQL 與筆數（供前端顯示）
                ExtractFunctionCallMeta(chatHistory, response);

                // 寫入 AI 回覆到 chatHistory
                chatHistory.AddAssistantMessage(assistantReply);

                // 透過 ChatHistoryTruncationReducer 壓縮歷史（超過閾值才觸發）
                var reduced = await _historyReducer.ReduceAsync(chatHistory);
                if (reduced is not null)
                    chatHistory = new ChatHistory(reduced);

                SaveSessionChatHistory(resolvedSessionId, chatHistory);
            }
            catch (AIChatOutOfScopeException)
            {
                // 移除剛加入的 user message，避免污染歷史
                if (chatHistory.Count > 0 && chatHistory.Last().Role == AuthorRole.User)
                    chatHistory.RemoveAt(chatHistory.Count - 1);

                response.Success = false;
                response.Answer = "抱歉，您的問題超出了我的查詢範圍。";
                response.ErrorMessage = "問題超出可查詢範圍。";
            }
            catch (AIChatSecurityException secEx)
            {
                if (chatHistory.Count > 0 && chatHistory.Last().Role == AuthorRole.User)
                    chatHistory.RemoveAt(chatHistory.Count - 1);

                response.Success = false;
                response.ErrorMessage = secEx.Message;
                response.Answer = $"⚠️ 安全性限制：{secEx.Message}";
            }
            catch (Exception ex)
            {
                if (chatHistory.Count > 0 && chatHistory.Last().Role == AuthorRole.User)
                    chatHistory.RemoveAt(chatHistory.Count - 1);

                response.Success = false;
                response.ErrorMessage = ex.ToString();
                response.Answer = $"很抱歉，處理您的問題時發生錯誤：{ex.Message}";
                AICommon.LogError(ex, new { SessionId = resolvedSessionId, UserMessage = userMessage });
            }

            return response;
        }

        // ─────────────────────────────────────────────
        // Session 歷史管理（存放 ChatHistory 物件）
        // ─────────────────────────────────────────────

        private ChatHistory GetSessionChatHistory(string sessionId)
        {
            string cacheKey = $"AIChat_Session_{sessionId}";
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(SessionCacheMinutes);
                var history = new ChatHistory();
                history.AddSystemMessage(GetOrchestratorSystemPrompt());
                return history;
            })!;
        }

        private void SaveSessionChatHistory(string sessionId, ChatHistory chatHistory)
        {
            string cacheKey = $"AIChat_Session_{sessionId}";
            _cache.Set(cacheKey, chatHistory, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(SessionCacheMinutes)
            });
        }

        /// <summary>清除指定 Session 的對話歷史</summary>
        public void ClearSession(string sessionId)
        {
            _cache.Remove($"AIChat_Session_{sessionId}");
        }

        // ─────────────────────────────────────────────
        // 工具方法：從 chatHistory 的 Function 結果中萃取 SQL 與筆數
        // ─────────────────────────────────────────────

        /// <summary>
        /// 在 Kernel Function Calling 執行後，從 chatHistory 的 Tool/Function 訊息中，
        /// 嘗試萃取 SQL（供前端 SQL 展開區塊使用）與 rowCount
        /// </summary>
        private void ExtractFunctionCallMeta(ChatHistory chatHistory, AIChatResponseVO response)
        {
            // 直接從 Plugin 讀取最後一次執行的 SQL 與 FunctionName
            if (_historyFileQueryPlugin.LastExecutedSqls.Count > 0)
            {
                response.GeneratedSql = string.Join("\n\n", _historyFileQueryPlugin.LastExecutedSqls);
                response.FunctionName = _historyFileQueryPlugin.LastFunctionName;
                return;
            }
            if (_quotationQueryPlugin.LastExecutedSqls.Count > 0)
            {
                response.GeneratedSql = string.Join("\n\n", _quotationQueryPlugin.LastExecutedSqls);
                response.FunctionName = _quotationQueryPlugin.LastFunctionName;
                return;
            }

            // 保留 fallback：從 chatHistory 的 Tool 訊息嘗試解析 SQL
            foreach (var msg in chatHistory.Reverse())
                {
                    if (msg.Role != AuthorRole.Tool)
                        continue;

                    string? content = msg.Content;
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("steps", out var stepsProp) && stepsProp.ValueKind == JsonValueKind.Array)
                        {
                            var sqlParts = new List<string>();
                            foreach (var step in stepsProp.EnumerateArray())
                            {
                                if (step.TryGetProperty("sql", out var s))
                                {
                                    var sqlStr = s.GetString();
                                    if (!string.IsNullOrWhiteSpace(sqlStr))
                                        sqlParts.Add(sqlStr);
                                }
                            }
                            if (sqlParts.Count > 0)
                                response.GeneratedSql = string.Join("\n\n", sqlParts);
                        }
                        else if (root.TryGetProperty("sql", out var sqlProp))
                        {
                            response.GeneratedSql = sqlProp.GetString() ?? string.Empty;
                        }
                    }
                    catch
                    {
                        // JSON 解析失敗忽略即可
                    }

                    break;
                }

            // 從 Assistant 訊息中擷取 Kernel Function 名稱（fallback）
            foreach (var msg in chatHistory.Reverse())
            {
                if (msg.Role != AuthorRole.Assistant)
                    continue;

                var functionCallContent = msg.Items?.OfType<FunctionCallContent>().FirstOrDefault();
                if (functionCallContent != null)
                {
                    response.FunctionName = functionCallContent.FunctionName;
                    break;
                }
            }
        }

        private static string BuildResultSummary(string queryResultJson, int rowCount, int maxRows = 50)
        {
            if (rowCount <= maxRows)
                return queryResultJson;

            try
            {
                using var doc = JsonDocument.Parse(queryResultJson);
                var limitedRows = doc.RootElement.EnumerateArray().Take(maxRows).ToList();
                string limited = JsonSerializer.Serialize(limitedRows);
                return $"{limited}（以上僅顯示前 {maxRows} 筆，共 {rowCount} 筆）";
            }
            catch
            {
                return queryResultJson;
            }
        }
    }

    /// <summary>問題超出可查詢資料表範圍</summary>
    public class AIChatOutOfScopeException : Exception
    {
        public AIChatOutOfScopeException() : base("問題超出可查詢範圍。") { }
    }

    /// <summary>SQL 安全性例外</summary>
    public class AIChatSecurityException : Exception
    {
        public AIChatSecurityException(string message) : base(message) { }
    }
}
