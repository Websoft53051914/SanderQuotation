using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("EsFileTransferMapping")]
    public class EsFileTransferMappingEntity : SP_BaseEntity
    {
        public string Type { set; get; }

        public string SortNo { set; get; }

        public string Priority { set; get; }

        /// <summary>
        /// 系統編號 (自動產生)
        /// </summary>
        public string TransferMappingCode { set; get; }

        // 匯入範本檔案名稱
        public string ExampleFileName { set; get; }

        // 匯入範本檔案類型
        public int ExampleFileType { set; get; }

        // NAS 檔案路徑
        public string SrcNasFilePath { set; get; }

        // 備註
        public string? Description { set; get; }

        // 實體檔案名稱
        public string? FileName { set; get; }
    }
}
