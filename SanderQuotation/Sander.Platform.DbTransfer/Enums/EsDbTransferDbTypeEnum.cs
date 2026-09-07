using System.ComponentModel;

namespace Sander.Platform.DbTransfer
{
    /// <summary>
    /// 資料庫轉檔連線的資料庫類型。數值寫入 ESDbTransfer.DbType，不可變更。
    /// </summary>
    public enum EsDbTransferDbTypeEnum
    {
        [Description("PostgreSQL")]
        PostgreSQL = 1,

        [Description("MSSQL")]
        MSSQL = 2
    }
}
