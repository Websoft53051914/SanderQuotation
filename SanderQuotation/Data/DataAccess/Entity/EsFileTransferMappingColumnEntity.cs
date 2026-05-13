using Core.Utility.Base.Data.GuidId;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("EsFileTransferMappingColumn")]
    public class EsFileTransferMappingColumnEntity : BaseEntity
    {
        public int Status { set; get; }

        public string Type { set; get; }

        public string SortNo { set; get; }

        public string Priority { set; get; }

        public DateTime? CreatedAt { set; get; }

        public DateTime? UpdatedAt { set; get; }

        public string CreatedBy { set; get; }

        public string UpdatedBy { set; get; }

        /// <summary>
        /// 系統編號 自動生成
        /// </summary>
        public string EsFileTransferMappingColumnID {  set; get; }

        // FK → EsFileTransferMappingEntity
        public string TransferMappingCode { set; get; }

        // 工作表名稱
        public string SrcSheetName { set; get; }

        // 工作表索引（0-based）
        public int SrcSheetIndex { set; get; }

        // 對應sheet的標題列起始行（1-based）
        public int HeaderRowIndex { set; get; }

        /// <summary>
        /// 對應資料庫設定代碼
        /// </summary>
        public string DBTransferMappingCode { set; get; }

        // 對應資料表名稱
        public string TargetTableName { set; get; }

        // 檔案(Excel/csv)欄位名稱
        public string SrcFileColumnName { set; get; }

        // 資料表對應欄位名稱
        public string TargetTableColumnName { set; get; }

        // 是否為主鍵
        public bool IsPrimaryKey { set; get; }

        // 是否加密
        public bool IsEncrypt { set; get; }

        // 當來源欄位值為 Null 時填入的預設值
        public string DefaultValue { set; get; }

        // 篩選條件（JSON 字串，存儲條件列表或手動 SQL）
        public string FilterCondition { set; get; }

        // 篩選條件模式：'builder'=條件列 / 'manual'=手寫 SQL
        public string FilterMode { set; get; }
    }
}
