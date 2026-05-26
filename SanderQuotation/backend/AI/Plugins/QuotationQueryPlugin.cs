using backend.Common;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Const.Enums;

namespace backend.AI.Plugins
{
    /// <summary>
    /// Kernel Function Plugin：查價相關查詢工具
    /// 負責：理解使用者意圖 → 生成對應 SQL → 呼叫 SqlExecutorPlugin 執行
    /// 涵蓋 BOM 查價、料品、採購紀錄、上傳檔案等相關資料表查詢
    /// </summary>
    public partial class QuotationQueryPlugin
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

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="chatService"></param>
        /// <param name="sqlExecutor"></param>
        /// <param name="embedding"></param>
        public QuotationQueryPlugin(
            IChatCompletionService chatService,
            SqlExecutorPlugin sqlExecutor,
            ManualBatchEmbedding embedding)
        {
            _chatService = chatService;
            _sqlExecutor = sqlExecutor;
            _embedding = embedding;
        }
    }

    public partial class QuotationQueryPlugin
    {
        // ─────────────────────────────────────────────
        // Schema & 查詢邏輯描述（System Prompt）
        // ─────────────────────────────────────────────

        private string GetQuerySystemPrompt()
        {
            return $@"
你是一個專精於 PostgreSQL 的資料庫專家，專門負責「BOM 查價、料品管理、採購紀錄」相關的查詢。

# 你負責的資料表

## esfiletransferupload（轉入檔案上傳）
- id (uuid, 主鍵)
- uploadid (uuid)：檔案儲存代號
- filename (varchar)：原始檔案名稱
- quotationqty (int4)：報價數量
- customercode (varchar)：ERP 客戶代碼
- manualcustomername (varchar)：客戶名稱（手動）
- prodno (varchar)：產品料號
- processstatus (int4)：執行狀態（1=未轉檔, 2=已轉檔, 3=未查料, 4=已查料, 5=已查價, 99=轉檔失敗）
- esfiletransfermappingid (uuid)：匯入規則 ID
- status (int4)：0=停用, 1=啟用, 9=刪除
- createdby (varchar), updatedby (varchar), createdat (timestamp), updatedat (timestamp)

## bomfilecontent（BOM 表內容）
- id (uuid, 主鍵)
- uploadid (uuid)：外鍵，關聯至 esfiletransferupload.uploadid
- componentpart (text)：元件料號
- description (text)：元件描述
- qty (int4)：數量
- manufacturer (text)：廠商
- manufacturerpartnumber (text)：廠商型號（MPN）
- displaypart (text)：顯示用料號
- createdat (timestamp), updatedat (timestamp)

## sandermoduleitem（內部料品表）
- id (uuid, 主鍵)
- no (varchar)：內部料號
- description (varchar)：料品規格主欄位
- description2 (varchar)：料品規格次欄位
- longdesc (varchar)：MPN 主要欄位
- longdesc2 (varchar)：MPN 次要欄位＋備註
- itemcategorycode (varchar)：料品類別代碼
- flagneedextractkeyword (bool)：是否需要 AI 關鍵字抽取
- createdat (timestamp), updatedat (timestamp)

## sandermoduleitemvariant（料品 Variant 資料）
- id (uuid, 主鍵)
- itemno (varchar)：內部料號（關聯至 sandermoduleitem.no）
- code (varchar)：Variant Code
- description (varchar)：客戶承認之供應商型號
- description2 (varchar)：客戶承認之供應商型號（次）
- flagneedextractkeyword (int4)：0=否（直接用 customerapprovedpartcsv），非 0=是
- customerapprovedpartcsv (text)：客戶承認料（多筆以「,」隔開）
- createdat (timestamp), updatedat (timestamp)

## reportitemcustomer（客戶代碼與 Variant Code 對照表）
- id (uuid, 主鍵)
- variantcode (varchar)：對應之 Variant 代碼
- customercode (varchar)：ERP 客戶代碼
- customername (varchar)：客戶簡稱
- createdat (timestamp), updatedat (timestamp)

## sandermodulepurchaseline（內部採購紀錄）⚠️ 資料量大
- id (uuid, 主鍵)
- no (varchar)：內部料號
- documentdate (timestamp)：單據日期
- buyfromvendorno (varchar)：供應商代碼
- buyfromvendorname (varchar)：供應商名稱
- unitcost (numeric)：單價（原幣）
- unitcostlcy (numeric)：單價（本幣 TWD）
- quantity (int4)：採購數量
- currencycode (varchar)：幣別代碼
- description2 (varchar)：採購當下之供應商型號
- createdat (timestamp), updatedat (timestamp)

## tb_bomfilequotation（BOM 查價結果）
- id (uuid, 主鍵)
- bomfilecontentid (uuid)：外鍵，關聯至 bomfilecontent.id
- no (varchar)：採購型號（內部料號）
- status (int4)：0=停用, 1=啟用, 9=刪除
- isrecommendedno (bool)：是否為建議採購型號
- matchcategory (int4)：比對結果（1=完全命中, 2=建議料號, 3=未命中）
- matchfield (varchar)：比對命中欄位（MPN / Component Part / 規格）
- isfilterbycustomerapprovedpart (bool)：是否套用客戶承認料過濾
- customerapprovedpartcsv (text)：使用的客戶承認料清單
- internalpurchaseorderdate (timestamp)：內部採購單日期
- internalunitpriceoriginalcurrency (numeric)：內部單價（原幣）
- internalunitpricetwd (numeric)：內部單價（台幣）
- internalquantity (int4)：內部數量
- internalcurrency (varchar)：內部幣別
- internalsuppliername (varchar)：內部供應商名稱
- internalsuppliercode (varchar)：內部供應商代碼
- internalitemdescription2 (varchar)：採購型號 Description2
- internallowminprice (numeric)：AI 分群低價群最低價
- internallowmaxprice (numeric)：AI 分群低價群最高價
- internalhighminprice (numeric)：AI 分群高價群最低價
- internalhighmaxprice (numeric)：AI 分群高價群最高價
- internalquotationdate (timestamp)：內部查價日期
- externalquotationdate (timestamp)：外部查價日期
- externalunitpriceoriginalcurrency (numeric)：外部單價（原幣）
- externalunitpricetwd (numeric)：外部單價（台幣）
- externalmoq (int4)：外部 MOQ
- externalcurrency (varchar)：外部幣別
- externalsuppliername (varchar)：外部供應商名稱
- externalstock (int4)：外部庫存量
- externalscenario (int4)：外部查價情境（1=情境A優先名單, 2=情境B後備）
- createdby (varchar), updatedby (varchar), createdat (timestamp), updatedat (timestamp)

## tb_bomfilequotationexternalhistory（BOM 外部查價歷史）
- id (uuid, 主鍵)
- quotationdate (timestamp)：查價日期
- manufacturerpartnumber (text)：廠商型號（MPN）
- suppliername (varchar)：供應商名稱
- unitpriceoriginalcurrency (numeric)：單價（原幣）
- unitpricetwd (numeric)：單價（台幣）
- moq (int4)：最小訂購量
- currency (varchar)：幣別
- stock (int4)：庫存量
- status (int4)：0=停用, 1=啟用, 9=刪除
- createdby (varchar), updatedby (varchar), createdat (timestamp), updatedat (timestamp)

## tb_bomfilequotationother（BOM 現貨優惠價結果）
- id (uuid, 主鍵)
- bomfilecontentid (uuid)：外鍵，關聯至 bomfilecontent.id
- sourcetype (int4)：來源類型
- quotationdate (timestamp)：查價日期
- unitpriceoriginalcurrency (numeric)：單價（原幣）
- unitpricetwd (numeric)：單價（台幣）
- moq (int4)：最小訂購量
- currency (varchar)：幣別
- suppliername (varchar)：供應商名稱
- status (int4)：0=停用, 1=啟用, 9=刪除
- createdby (varchar), updatedby (varchar), createdat (timestamp), updatedat (timestamp)

## tb_bomfiledecisionlog（BOM 決策歷程）
- id (uuid, 主鍵)
- bomfilecontentid (uuid)：外鍵，關聯至 bomfilecontent.id
- stage (int4)：階段（1=查料, 2=內部查價, 3=外部查價, 4=Mouser查價, 5=DigiKey查價）
- step (int4)：步驟
- message (text)：紀錄訊息
- status (int4)：0=停用, 1=啟用, 9=刪除
- createdby (varchar), updatedby (varchar), createdat (timestamp), updatedat (timestamp)

## tb_sandermoduleitemkeyword（內部料號關鍵字）
- id (uuid, 主鍵)
- no (varchar)：內部料號
- columnname (varchar)：欄位名稱（對應 sandermoduleitem 的欄位）
- keyword (varchar)：關鍵字
- keywordembedding (vector)：關鍵字向量（用於語意相似度搜尋）
- status (int4)：0=停用, 1=啟用, 9=刪除
- createdby (varchar), updatedby (varchar), createdat (timestamp), updatedat (timestamp)

# 查詢規則（極重要）
1. 查詢有 status 欄位的資料表時，必須加上 `status = {(int)StatusEnum.Enabled}`，除非使用者明確要求查詢其他狀態。
2. 現在時間：{DateTime.Now:yyyy/MM/dd HH:mm:ss} {Method.GetDayName(DateTime.Now.DayOfWeek)}。本週範圍：{Method.GetWeekStart(DateTime.Now):yyyy/MM/dd}（週一）～ {Method.GetWeekEnd(DateTime.Now):yyyy/MM/dd}（週日）。
3. 只能生成 SELECT，絕對禁止 INSERT / UPDATE / DELETE / DROP 等。
4. bomfilecontent 沒有 status 欄位，不需要加 status 過濾。
5. sandermoduleitem、sandermoduleitemvariant、reportitemcustomer、sandermodulepurchaseline 沒有 status 欄位，不需要 status 過濾。
6. 若問題需要語意相似度搜尋，SQL 中請使用 {VectorPlaceholder} 作為向量佔位符，系統會自動替換成實際向量值，限定相似度一定要大於 0.7。

# 向量查詢範例（適用於「找跟 XX 相關的料品關鍵字」類型的問題）
SELECT a.no, a.description, b.columnname, b.keyword,
       (1 - (b.keywordembedding <=> '{VectorPlaceholder}')) AS similarity
FROM sandermoduleitem a
INNER JOIN tb_sandermoduleitemkeyword b ON a.no = b.no
WHERE b.status = {(int)StatusEnum.Enabled}
  AND (1 - (b.keywordembedding <=> '{VectorPlaceholder}')) > 0.7
ORDER BY b.keywordembedding <=> '{VectorPlaceholder}'
LIMIT 10;

# 輸出格式（極重要）
- 若問題可用一條 SQL 回答：輸出一個 ```sql ... ``` 區塊。
- 若問題需要多條 SQL 依序執行（例如：先查出 id 清單，再用 id 查詳細資料）：依序輸出多個 ```sql ... ``` 區塊，每個區塊一條 SQL。
- 不要有任何其他說明文字，只輸出 SQL 區塊。
";
        }

        // ─────────────────────────────────────────────
        // Kernel Function：查詢查價相關資料
        // ─────────────────────────────────────────────

        /// <summary>
        /// 根據使用者對查價、料品、採購紀錄的自然語言問題，生成並依序執行一或多條 SQL 查詢，回傳包含每步結果的 JSON。
        /// </summary>
        [KernelFunction("query_quotation_data")]
        [Description("查詢 BOM 查價、料品管理、採購紀錄等相關資料，例如：查詢某 BOM 的查價結果、某料號的歷史採購價格、外部查價歷史、BOM 決策歷程、特定客戶對應的 Variant、依語意尋找相關料品關鍵字（向量搜尋）。支援需要多次 SQL 才能回答的問題。回傳包含 steps（每步 sql/rowCount/data）與 finalData 的 JSON 字串。")]
        public async Task<string> QueryQuotationDataAsync(
            [Description("使用者關於查價、料品或採購紀錄的自然語言問題，例如：『某料號最近的內部查價結果』、『顯示某 BOM 的決策歷程』、『找跟電容相關的料品』、『哪些 BOM 已查價完成』")] string question)
        {
            // Step 1：呼叫 AI 根據 Schema 描述生成一或多條 SQL
            ChatHistory tempHistory = new();
            tempHistory.AddSystemMessage(GetQuerySystemPrompt());
            tempHistory.AddUserMessage(question);

            var result = await _chatService.GetChatMessageContentAsync(tempHistory);
            string aiContent = result.Content ?? string.Empty;

            List<string> sqlList = ExtractAllSqlFromMarkdown(aiContent);

            if (sqlList.Count == 0)
                return JsonSerializer.Serialize(new { steps = Array.Empty<object>(), finalData = Array.Empty<object>(), error = "AI 無法為此問題生成有效的 SQL。" });

            LastFunctionName = "query_quotation_data";
            LastExecutedSqls = new List<string>();

            List<object> steps = new();
            string previousResultJson = string.Empty;

            for (int i = 0; i < sqlList.Count; i++)
            {
                string rawSql = sqlList[i];

                if (i > 0 && !string.IsNullOrWhiteSpace(previousResultJson))
                    rawSql = await RefineSqlWithPreviousResultAsync(rawSql, previousResultJson, question);

                string executableSql = await ResolveVectorPlaceholderAsync(rawSql, question);
                LastExecutedSqls.Add(executableSql);
                string rawResult = await _sqlExecutor.ExecuteSqlAsync(executableSql);
                previousResultJson = rawResult;

                try
                {
                    using JsonDocument doc = JsonDocument.Parse(rawResult);
                    steps.Add(new
                    {
                        step = i + 1,
                        sql = rawSql,
                        rowCount = doc.RootElement.TryGetProperty("rowCount", out JsonElement rc) ? rc.GetInt32() : 0,
                        data = JsonSerializer.Deserialize<object>(
                            doc.RootElement.TryGetProperty("data", out JsonElement d) ? d.GetRawText() : "[]")
                    });
                }
                catch
                {
                    steps.Add(new { step = i + 1, sql = rawSql, rawResult });
                }
            }

            object? finalData = null;
            if (steps.Count > 0)
                finalData = steps.Last().GetType().GetProperty("data")?.GetValue(steps.Last());

            return JsonSerializer.Serialize(new { steps, finalData }, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        // ─────────────────────────────────────────────
        // 工具方法
        // ─────────────────────────────────────────────

        /// <summary>當多步驟查詢時，請 AI 將前步結果融入當前 SQL（補充 IN 條件等）</summary>
        private async Task<string> RefineSqlWithPreviousResultAsync(string currentSql, string previousResultJson, string originalQuestion)
        {
            ChatHistory refineHistory = new();
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
            List<string> results = new();
            var matches = Regex.Matches(content, @"```sql\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                string sql = m.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(sql))
                    results.Add(sql);
            }
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
