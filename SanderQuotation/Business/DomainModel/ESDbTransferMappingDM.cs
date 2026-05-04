namespace Business.DomainModel
{
    public class ESDbTransferMappingDM : BaseDM
    {
        // 映射代碼
        public string TransferMappingCode { set; get; }

        // 來源資料庫連線 (FK to ESDbTransfer)
        public string SrcDbTransferCode { set; get; }

        // 來源資料庫名稱 (display only)
        public string SrcTransferName { set; get; }

        // 來源 TABLE 名稱
        public string SrcTableName { set; get; }

        // 目的資料庫連線 (FK to ESDbTransfer)
        public string DstDbTransferCode { set; get; }

        // 目的資料庫名稱 (display only)
        public string DstTransferName { set; get; }

        // 目標 TABLE 名稱
        public string DstTableName { set; get; }

        // 備註
        public string Description { set; get; }

        // 欄位對應明細
        public List<ESDbTransferMappingColumnDM> Columns { set; get; } = new();

        // 篩選條件（JSON 字串，儲存條件列或手寫 SQL）
        public string FilterCondition { set; get; }

        // 篩選條件模式：'manual'=手寫SQL, 'builder'=條件列
        public string FilterMode { set; get; }
    }
}

