namespace Sander.Platform.DbTransfer
{
    /// <summary>資料庫連線設定（對應 ESDbTransfer 主檔）。</summary>
    public class DbTransferConnectionConfig
    {
        public string TransferCode { get; set; } = string.Empty;
        public string TransferName { get; set; } = string.Empty;
        public string DbType { get; set; } = string.Empty;
        public string DbHost { get; set; } = string.Empty;
        public string DbPort { get; set; } = string.Empty;
        public string DbName { get; set; } = string.Empty;
        public string DbUser { get; set; } = string.Empty;
        public string DbPassword { get; set; } = string.Empty;
    }
}
