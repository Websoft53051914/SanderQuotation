using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    /// <summary>
    /// 轉入檔案上傳
    /// </summary>
    [Table("EsFileTransferUpload")]
    public class EsFileTransferUploadEntity : SP_BaseEntity
    {
        /// <summary>
        /// 檔案儲存代號
        /// </summary>
        public Guid UploadId { set; get; }

        /// <summary>
        /// 原始檔案名稱
        /// </summary>
        public string FileName { set; get; } = string.Empty;

        /// <summary>
        /// 匯入規則 FK (EsFileTransferMapping.Id)
        /// </summary>
        public Guid? EsFileTransferMappingId { set; get; }

        /// <summary>
        /// 報價數量
        /// </summary>
        public int? QuotationQty { set; get; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string? CustomerCode { set; get; }

        /// <summary>
        /// 產品料號
        /// </summary>
        public string? ProdNo { set; get; }

        /// <summary>
        /// 執行狀態
        /// </summary>
        public int? ProcessStatus { set; get; }

        /// <summary>
        /// 手動輸入客戶名稱（CustomerCode 為空時使用）
        /// </summary>
        public string? ManualCustomerName { set; get; }
    }
}
