using Data.DataAccess.Entity;

namespace Data.DataAccess.DTO
{
    public class EsFileTransferUploadDTO : EsFileTransferUploadEntity
    {
        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 匯入規則代碼 (由 JOIN EsFileTransferMapping 帶入)
        /// </summary>
        public string? TransferMappingCode { get; set; }

        /// <summary>
        /// BOM 料項數 (由子查詢帶入)
        /// </summary>
        public int? ItemCount { get; set; }
    }
}
