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
你是一個專精於 PostgreSQL 的資料庫專家，專門負責「歷史資料上傳檔案」相關的查詢。

# 你負責的資料表

## historyfile（歷史資料檔案主表）
- id (uuid, 主鍵)
- filename (varchar(100))：上傳的檔案名稱
- uploadid (varchar(36))：檔案儲存代號
- createdat (timestamp)：建立時間
- updatedat (timestamp)：更新時間
- createdby (varchar(100))：建立帳號
- updatedby (varchar(100))：更新帳號
- status (int4)：狀態 0=停用, 1=啟用, 9=刪除
- filesummary (text)：檔案內容摘要

## embeddedhistoryfile（歷史資料檔案摘要向量表）
- id (uuid, 主鍵)
- embedding (vector)：檔案摘要向量（用於語意相似度搜尋）
- status (int4)：1=啟用, 9=刪除
- historyfileid (uuid)：外鍵，關聯至 historyfile.id
- createdby (varchar(100)), updatedby (varchar(100))
- createdat (timestamp), updatedat (timestamp)

# 查詢規則（極重要）
1. 查詢 historyfile 時，必須加上 `status = {(int)StatusEnum.Enabled}`。
2. 查詢 embeddedhistoryfile 時，必須加上 `status = {(int)StatusEnum.Enabled}`。
3. 現在時間：{DateTime.Now:yyyy/MM/dd HH:mm:ss}。相對時間用 PostgreSQL 函數計算，例如 CURRENT_DATE - INTERVAL '1 day'。
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
只輸出 SQL 語句本身，用 ```sql 與 ``` 包裹，不要有任何其他說明文字。
";
        }

        // ─────────────────────────────────────────────
        // Kernel Function：查詢歷史上傳檔案
        // ─────────────────────────────────────────────

        /// <summary>
        /// 根據使用者對歷史上傳檔案的自然語言問題，生成並執行 SQL 查詢，回傳結果 JSON
        /// </summary>
        [KernelFunction("query_history_files")]
        [Description("查詢歷史上傳檔案的相關資料，例如：查詢特定日期的上傳筆數、搜尋特定帳號的上傳紀錄、依語意尋找與特定主題相關的檔案（向量搜尋）。回傳包含 rowCount 與 data 的 JSON 字串。")]
        public async Task<string> QueryHistoryFilesAsync(
            [Description("使用者關於歷史上傳檔案的自然語言問題，例如：『昨天有幾筆上傳？』、『找天氣相關的檔案』")] string question)
        {
            // Step 1：呼叫 AI 根據 Schema 描述生成 SQL
            var tempHistory = new ChatHistory();
            tempHistory.AddSystemMessage(GetQuerySystemPrompt());
            tempHistory.AddUserMessage(question);

            var result = await _chatService.GetChatMessageContentAsync(tempHistory);
            string rawSql = ExtractSqlFromMarkdown(result.Content ?? string.Empty);

            if (string.IsNullOrWhiteSpace(rawSql))
                return JsonSerializer.Serialize(new { rowCount = 0, sql = "", data = Array.Empty<object>(), error = "AI 無法為此問題生成有效的 SQL。" });

            // Step 2：若 SQL 含向量佔位符，取得 Embedding 並替換
            string executableSql = await ResolveVectorPlaceholderAsync(rawSql, question);

            // Step 3：直接呼叫 SqlExecutorPlugin 執行 SQL
            string rawResult = await _sqlExecutor.ExecuteSqlAsync(executableSql);

            // Step 4：將 sql 欄位附加到回傳結果，供前端 SQL 展開區塊使用
            try
            {
                using var doc = JsonDocument.Parse(rawResult);
                var dict = new Dictionary<string, object?>
                {
                    ["sql"] = rawSql,
                    ["rowCount"] = doc.RootElement.TryGetProperty("rowCount", out var rc) ? rc.GetInt32() : 0,
                    ["data"] = JsonSerializer.Deserialize<object>(
                        doc.RootElement.TryGetProperty("data", out var d) ? d.GetRawText() : "[]")
                };
                return JsonSerializer.Serialize(dict, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                return rawResult;
            }
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
