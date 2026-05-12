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
    }
}
