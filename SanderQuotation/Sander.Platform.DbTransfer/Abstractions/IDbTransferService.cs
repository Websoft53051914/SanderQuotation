namespace Sander.Platform.DbTransfer
{
    /// <summary>
    /// 開源端（TransferJob、TableExcel、排程設定）呼叫 DbTransfer 的唯一入口。
    /// </summary>
    public interface IDbTransferService
    {
        /// <summary>依對應規則代碼執行 DB→DB 轉檔。</summary>
        DbTransferExecuteResult ExecuteTransfer(string transferMappingCode, string secretKey, string secretIV);

        /// <summary>依連線代碼（TransferCode）取得資料庫連線設定。</summary>
        DbTransferConnectionConfig? GetDbTransferConfig(string dbTransferCode);

        /// <summary>啟用中的資料表轉檔規則，供排程下拉。</summary>
        IReadOnlyList<DbTransferOptionItem> GetDbTransferOptions();
    }
}
