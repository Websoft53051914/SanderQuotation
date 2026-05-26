using backend.Common;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Const.Enums;

namespace backend.AI.Plugins
{
    /// <summary>
    /// Kernel Function Plugin：歷史資料上傳檔案查詢工具
    /// 負責：理解使用者意圖 → 生成對應 SQL → 呼叫 SqlExecutorPlugin 執行
    /// 若需要擴充其他資料表，仿照此 Plugin 新增即可
    /// </summary>
    public class HistoryFileQueryPlugin
    {
        private readonly IChatCompletionService _chatService;
        private readonly SqlExecutorPlugin _sqlExecutor;
        private readonly ManualBatchEmbedding _embedding;

        // 向量佔位符識別字串（AI 生成 SQL 時使用，此處替換成實際向量）
        private const string VectorPlaceholder = "{QUERY_VECTOR}";

        /// <summary>最後一次呼叫所執行的所有 SQL（多步驟時為多筆）</summary>
        public List<string> LastExecutedSqls { get; private set; } = new();

        /// <summary>最後一次呼叫的 Kernel Function 名稱</summary>
        public string LastFunctionName { get; private set; } = string.Empty;

        public HistoryFileQueryPlugin(
            IChatCompletionService chatService,
            SqlExecutorPlugin sqlExecutor,
            ManualBatchEmbedding embedding)
        {
            _chatService = chatService;
            _sqlExecutor = sqlExecutor;
            _embedding = embedding;
        }

        // ─────────────────────────────────────────────
        // Schema & 查詢邏輯描述（System Prompt 搬移至此）
        // ─────────────────────────────────────────────
        private string GetQuerySystemPrompt()
        {
            return $@"
你是一個專精於 PostgreSQL 的資料庫專家，專門負責「AI歷史資料上傳檔案」相關的查詢。

# 你負責的資料表

## historyfile（AI歷史資料檔案主表）
- id (uuid, 主鍵)
- filename (varchar(100))：上傳的檔案名稱
- uploadid (varchar(36))：檔案儲存代號
- createdat (timestamp)：建立時間
- updatedat (timestamp)：更新時間
- createdby (varchar(100))：建立帳號
- updatedby (varchar(100))：更新帳號
- status (int4)：狀態 0=停用, 1=啟用, 9=刪除
- filesummary (text)：檔案內容摘要

## embeddedhistoryfile（AI歷史資料檔案摘要向量表）
- id (uuid, 主鍵)
- embedding (vector)：檔案摘要向量（用於語意相似度搜尋）
- status (int4)：1=啟用, 9=刪除
- historyfileid (uuid)：外鍵，關聯至 historyfile.id
- createdby (varchar(100)), updatedby (varchar(100))
- createdat (timestamp), updatedat (timestamp)

# 查詢規則（極重要）
1. 查詢 historyfile 時，必須加上 `status = {(int)StatusEnum.Enabled}`。
2. 查詢 embeddedhistoryfile 時，必須加上 `status = {(int)StatusEnum.Enabled}`。
3. 現在時間：{DateTime.Now:yyyy/MM/dd HH:mm:ss} {Method.GetDayName(DateTime.Now.DayOfWeek)}。本週範圍：{Method.GetWeekStart(DateTime.Now):yyyy/MM/dd}（週一）～ {Method.GetWeekEnd(DateTime.Now):yyyy/MM/dd}（週日），「本週」或「這週」一律指此範圍。
4. 只能生成 SELECT，絕對禁止 INSERT / UPDATE / DELETE / DROP 等。
5. 若問題需要語意相似度搜尋，SQL 中請使用 {VectorPlaceholder} 作為向量佔位符，系統會自動替換成實際向量值，限定相似度一定要大於0.7。

# 向量查詢範例（適用於「找跟 XX 相關的檔案」類型的問題）
SELECT a.id, a.filename, a.filesummary, (1 - (b.embedding <=> '{VectorPlaceholder}')) AS similarity
FROM historyfile a
INNER JOIN embeddedhistoryfile b ON a.id = b.historyfileid
WHERE a.status = {(int)StatusEnum.Enabled}
  AND b.status = {(int)StatusEnum.Enabled}
  AND (1 - (b.embedding <=> '{VectorPlaceholder}')) > 0.7
ORDER BY b.embedding <=> '{VectorPlaceholder}'
LIMIT 10;

# 輸出格式（極重要）
- 若問題可用一條 SQL 回答：輸出一個 ```sql ... ``` 區塊。
- 若問題需要多條 SQL 依序執行（例如：先查出 id 清單，再用 id 查詳細資料；或先統計再篩選）：依序輸出多個 ```sql ... ``` 區塊，每個區塊一條 SQL，系統會自動依序執行並將前步結果傳入下一步。
- 不要有任何其他說明文字，只輸出 SQL 區塊。
";
        }

        // ─────────────────────────────────────────────
        // Kernel Function：查詢歷史上傳檔案
        // ─────────────────────────────────────────────

        /// <summary>
        /// 根據使用者對歷史上傳檔案的自然語言問題，生成並依序執行一或多條 SQL 查詢，回傳包含每步結果的 JSON。
        /// 支援多步驟查詢：AI 可生成多條 SQL，每步可參考前一步結果作為條件。
        /// </summary>
        [KernelFunction("query_history_files")]
        [Description("查詢歷史上傳檔案的相關資料，例如：查詢特定日期的上傳筆數、搜尋特定帳號的上傳紀錄、依語意尋找與特定主題相關的檔案（向量搜尋）。支援需要多次 SQL 才能回答的問題。回傳包含 steps（每步 sql/rowCount/data）與 finalData 的 JSON 字串。")]
        public async Task<string> QueryHistoryFilesAsync(
            [Description("使用者關於歷史上傳檔案的自然語言問題，例如：『昨天有幾筆上傳？』、『找天氣相關的檔案』、『誰上傳了最多檔案？』")] string question)
        {
            // Step 1：呼叫 AI 根據 Schema 描述生成一或多條 SQL
            var tempHistory = new ChatHistory();
            tempHistory.AddSystemMessage(GetQuerySystemPrompt());
            tempHistory.AddUserMessage(question);

            var result = await _chatService.GetChatMessageContentAsync(tempHistory);
            string aiContent = result.Content ?? string.Empty;

            var sqlList = ExtractAllSqlFromMarkdown(aiContent);

            if (sqlList.Count == 0)
                return JsonSerializer.Serialize(new { steps = Array.Empty<object>(), finalData = Array.Empty<object>(), error = "AI 無法為此問題生成有效的 SQL。" });

            // 記錄本次呼叫的 function 名稱與 SQL
            LastFunctionName = "query_history_files";
            LastExecutedSqls = new List<string>();

            // Step 2
            var steps = new List<object>();
            string previousResultJson = string.Empty;

            for (int i = 0; i < sqlList.Count; i++)
            {
                string rawSql = sqlList[i];

                // 多步驟時，請 AI 將前步結果注入當前 SQL
                if (i > 0 && !string.IsNullOrWhiteSpace(previousResultJson))
                    rawSql = await RefineSqlWithPreviousResultAsync(rawSql, previousResultJson, question);

                string executableSql = await ResolveVectorPlaceholderAsync(rawSql, question);
                LastExecutedSqls.Add(executableSql);
                string rawResult = await _sqlExecutor.ExecuteSqlAsync(executableSql);
                previousResultJson = rawResult;

                try
                {
                    using var doc = JsonDocument.Parse(rawResult);
                    steps.Add(new
                    {
                        step = i + 1,
                        sql = rawSql,
                        rowCount = doc.RootElement.TryGetProperty("rowCount", out var rc) ? rc.GetInt32() : 0,
                        data = JsonSerializer.Deserialize<object>(
                            doc.RootElement.TryGetProperty("data", out var d) ? d.GetRawText() : "[]")
                    });
                }
                catch
                {
                    steps.Add(new { step = i + 1, sql = rawSql, rawResult });
                }
            }

            // 最後一步的 data 作為 finalData
            object? finalData = null;
            if (steps.Count > 0)
                finalData = steps.Last().GetType().GetProperty("data")?.GetValue(steps.Last());

            return JsonSerializer.Serialize(new { steps, finalData }, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        /// <summary>當多步驟查詢時，請 AI 將前步結果融入當前 SQL（補充 IN 條件等）</summary>
        private async Task<string> RefineSqlWithPreviousResultAsync(string currentSql, string previousResultJson, string originalQuestion)
        {
            var refineHistory = new ChatHistory();
            refineHistory.AddSystemMessage(GetQuerySystemPrompt());
            refineHistory.AddUserMessage($@"
原始問題：{originalQuestion}

前一步 SQL 查詢結果（JSON）：
{previousResultJson}

請根據上述前步結果，修改下面的 SQL，使其能正確利用前步結果中的 id 或其他欄位作為條件（例如 IN (...)），以得出最終答案。
若 SQL 已足夠或無需修改，原樣輸出即可。

待修改 SQL：
```sql
{currentSql}
```
");
            var result = await _chatService.GetChatMessageContentAsync(refineHistory);
            string refined = ExtractSqlFromMarkdown(result.Content ?? string.Empty);
            return string.IsNullOrWhiteSpace(refined) ? currentSql : refined;
        }

        // ─────────────────────────────────────────────
        // 工具方法
        // ─────────────────────────────────────────────



        private static string ExtractSqlFromMarkdown(string content)
        {
            var match = Regex.Match(content, @"```sql\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            var selectMatch = Regex.Match(content, @"(SELECT[\s\S]+)", RegexOptions.IgnoreCase);
            return selectMatch.Success ? selectMatch.Groups[1].Value.Trim() : string.Empty;
        }

        /// <summary>從 AI 回覆中擷取所有 SQL 區塊（支援多條）</summary>
        private static List<string> ExtractAllSqlFromMarkdown(string content)
        {
            var results = new List<string>();
            var matches = Regex.Matches(content, @"```sql\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                var sql = m.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(sql))
                    results.Add(sql);
            }
            // 若沒有 markdown block，fallback 抓第一條 SELECT
            if (results.Count == 0)
            {
                var selectMatch = Regex.Match(content, @"(SELECT[\s\S]+)", RegexOptions.IgnoreCase);
                if (selectMatch.Success)
                    results.Add(selectMatch.Groups[1].Value.Trim());
            }
            return results;
        }

        private async Task<string> ResolveVectorPlaceholderAsync(string sql, string question)
        {
            if (!sql.Contains(VectorPlaceholder))
                return sql;

            float[] vector = await _embedding.GetQueryEmbeddingAsync(question);
            string vectorLiteral = $"[{string.Join(",", vector.Select(v => v.ToString("G")))}]";
            return sql.Replace(VectorPlaceholder, vectorLiteral);
        }
    }
}
