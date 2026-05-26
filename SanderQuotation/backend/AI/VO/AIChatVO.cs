namespace backend.AI.VO
{
    /// <summary>
    /// AI 對話 Request
    /// </summary>
    public class AIChatRequestVO
    {
        /// <summary>
        /// 對話 Session ID（前端產生後每輪帶入，為空時視為新對話）
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// 使用者輸入的自然語言問題
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI 對話 Response
    /// </summary>
    public class AIChatResponseVO
    {
        /// <summary>
        /// 對話 Session ID（前端需保存，下一輪帶回來）
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// AI 生成的 SQL 語句
        /// </summary>
        public string GeneratedSql { get; set; } = string.Empty;

        /// <summary>
        /// 本次呼叫的 Kernel Function 名稱
        /// </summary>
        public string? FunctionName { get; set; }

        /// <summary>
        /// 查詢結果的自然語言回答
        /// </summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// 查詢筆數
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 錯誤訊息（若失敗）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 單輪對話的歷史紀錄（存在 Server 端 Cache）
    /// </summary>
    public class ConversationTurn
    {
        /// <summary>使用者問題</summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>當輪執行的 SQL</summary>
        public string ExecutedSql { get; set; } = string.Empty;

        /// <summary>當輪查詢結果（JSON，最多保留 20 筆供 AI 追問參考）</summary>
        public string QueryResultJson { get; set; } = string.Empty;

        /// <summary>當輪查詢筆數</summary>
        public int RowCount { get; set; }

        /// <summary>AI 的自然語言回答</summary>
        public string Answer { get; set; } = string.Empty;
    }
}
