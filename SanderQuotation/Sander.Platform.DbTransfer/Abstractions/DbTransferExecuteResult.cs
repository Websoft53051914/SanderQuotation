namespace Sander.Platform.DbTransfer
{
    /// <summary>DB→DB 轉檔執行結果。</summary>
    public class DbTransferExecuteResult
    {
        public string JobStatus { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public int DataCount { get; set; }
        public int ErrorCount { get; set; }
        public List<DbTransferErrorLog> ErrorLogs { get; set; } = new();
    }

    public class DbTransferErrorLog
    {
        public string? Exception { get; set; }
        public string? Sql { get; set; }
    }

    public class DbTransferOptionItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
