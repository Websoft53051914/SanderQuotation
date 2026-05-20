namespace Business.DomainModel
{
    // EsFileTransferUploadDM.ProcessStatus 值對應參照 EsFileTransferUploadProcessStatusEnum；上傳時預設為 null，非有效資料，代表未經前端儲存作業，可能是檔案刪除或未儲存

    /// <summary>
    /// 轉入檔案上傳
    /// </summary>
    public partial class EsFileTransferUploadDM : BaseDM
    {
        /// <summary>
        /// 檔案儲存代號
        /// </summary>
        public Guid UploadId { set; get; }

        /// <summary>
        /// 原始檔案名稱
        /// </summary>
        public string? FileName { set; get; }

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

    public partial class EsFileTransferUploadDM
    {
        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 匯入規則代碼 (EsFileTransferMapping.TransferMappingCode，顯示用)
        /// </summary>
        public string? TransferMappingCode { set; get; }

        /// <summary>
        /// BOM 料項數
        /// </summary>
        public int? ItemCount { get; set; }
    }
}