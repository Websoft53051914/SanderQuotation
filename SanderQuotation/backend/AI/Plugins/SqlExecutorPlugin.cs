using Microsoft.SemanticKernel;
using Npgsql;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace backend.AI.Plugins
{
    /// <summary>
    /// Kernel Function Plugin：通用 SQL 執行工具（僅允許 SELECT 查詢）
    /// </summary>
    public class SqlExecutorPlugin
    {
        private readonly IConfiguration _config;

        private static readonly string[] _forbiddenKeywords =
            ["INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE", "CREATE", "REPLACE", "MERGE", "CALL", "EXEC"];

        public SqlExecutorPlugin(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 執行 SELECT SQL 並回傳 JSON 結果
        /// </summary>
        [KernelFunction("execute_sql")]
        [Description("執行唯讀的 PostgreSQL SELECT 查詢語句，回傳包含 rowCount（筆數）與 data（資料陣列）的 JSON 字串。此工具只接受 SELECT 查詢，任何修改資料的指令都會被拒絕。")]
        public async Task<string> ExecuteSqlAsync(
            [Description("要執行的合法 PostgreSQL SELECT 語句，不得包含 INSERT/UPDATE/DELETE/DROP 等修改指令")] string sql)
        {
            // 安全性雙重驗證
            ValidateSql(sql);

            string connectionString = _config["ConnectionStrings:AIChatConnection"]
                ?? throw new InvalidOperationException("找不到資料庫連線字串（ConnectionStrings:AIChatConnection）。");

            var rows = new List<Dictionary<string, object?>>();

            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string colName = reader.GetName(i);
                    object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                    // vector 型態無法直接序列化，轉成字串
                    if (value is not null && value.GetType().Name.Contains("Vector", StringComparison.OrdinalIgnoreCase))
                        value = value.ToString();

                    row[colName] = value;
                }
                rows.Add(row);
            }

            return JsonSerializer.Serialize(new
            {
                rowCount = rows.Count,
                data = rows
            }, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        // ─────────────────────────────────────────────
        // SQL 安全性驗證（防止任何寫入操作）
        // ─────────────────────────────────────────────
        private static void ValidateSql(string sql)
        {
            string sqlUpper = Regex.Replace(sql, @"'[^']*'", "''").ToUpperInvariant();

            foreach (var keyword in _forbiddenKeywords)
            {
                var pattern = $@"(?<![A-Z_]){Regex.Escape(keyword)}(?![A-Z_])";
                if (Regex.IsMatch(sqlUpper, pattern))
                    throw new AIChatSecurityException($"SQL 中包含禁止的關鍵字：{keyword}，僅允許 SELECT 查詢。");
            }

            if (!sqlUpper.TrimStart().StartsWith("SELECT"))
                throw new AIChatSecurityException("查詢語句必須以 SELECT 開頭。");
        }
    }
}
