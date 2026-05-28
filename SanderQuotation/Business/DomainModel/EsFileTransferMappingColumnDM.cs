namespace Business.DomainModel
{
    public class EsFileTransferMappingColumnDM //: BaseDM
    {
        /// <summary>
        /// 系統編號 自動生成
        /// </summary>
        public string EsFileTransferMappingColumnID { set; get; }

        /// <summary>
        /// FK → EsFileTransferMappingEntity
        /// </summary>
        public string TransferMappingCode { set; get; }

        /// <summary>
        /// 工作表名稱
        /// </summary>
        public string SrcSheetName { set; get; }

        /// <summary>
        /// 工作表索引（0-based）
        /// </summary>
        public int SrcSheetIndex { set; get; }

        /// <summary>
        /// 對應 sheet 的標題列起始行（1-based）
        /// </summary>
        public int HeaderRowIndex { set; get; }

        /// <summary>
        /// 對應資料庫設定代碼
        /// </summary>
        public string DBTransferMappingCode { set; get; }
        /// <summary>
        /// 對應資料表名稱
        /// </summary>
        public string TargetTableName { set; get; }

        //對應資料表名稱註解
        public string TargetTableNameComment { set; get; }

        /// <summary>
        /// 檔案 (Excel/csv) 欄位名稱
        /// </summary>
        public string SrcFileColumnName { set; get; }

        /// <summary>
        /// 資料表對應欄位名稱
        /// </summary>
        public string TargetTableColumnName { set; get; }

        /// <summary>
        /// 是否為主鍵
        /// </summary>
        public bool IsPrimaryKey { set; get; }

        /// <summary>
        /// 是否加密
        /// </summary>
        public bool IsEncrypt { set; get; }

        /// <summary>
        /// 當來源欄位值為 Null 時填入的預設值
        /// </summary>
        public string DefaultValue { set; get; }

        /// <summary>
        /// 篩選條件（JSON 字串）
        /// </summary>
        public string FilterCondition { set; get; }

        /// <summary>
        /// 篩選條件模式：'builder' 或 'manual'
        /// </summary>
        public string FilterMode { set; get; }
    }
}
