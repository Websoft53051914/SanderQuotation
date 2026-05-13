namespace ViewModel.QuotationResult
{
    /// <summary>
    /// 定時查價結果 — 編輯頁 Header VM（EsFileTransferUpload 資料）
    /// </summary>
    public class QuotationFileEditVM
    {
        /// <summary>主鍵（GUID）</summary>
        public Guid? Id { get; set; }

        /// <summary>BOM 檔案名稱</summary>
        public string FileName { get; set; } = "";

        /// <summary>客戶代碼</summary>
        public string? CustomerCode { get; set; }

        /// <summary>客戶名稱</summary>
        public string? CustomerName { get; set; }

        /// <summary>產品料號</summary>
        public string? ProdNo { get; set; }

        /// <summary>報價數量</summary>
        public int? QuotationQty { get; set; }

        /// <summary>建立日期（顯示用）</summary>
        public string CreatedAtText { get; set; } = "";

        /// <summary>最後更新（顯示用）</summary>
        public string UpdatedAtText { get; set; } = "";

        /// <summary>BOM 料項查價結果清單</summary>
        public List<QuotationItemVM> Items { get; set; } = new();
    }

    /// <summary>
    /// 重新查價 — 單筆料項請求 VM
    /// </summary>
    public class QuotationReQuotationItemVM
    {
        /// <summary>bomfilecontent.Id</summary>
        public Guid Id { get; set; }

        /// <summary>選定的採購型號</summary>
        public string? No { get; set; }
    }

    /// <summary>
    /// 重新查價 — 請求 VM
    /// </summary>
    public class QuotationReQuotationRequestVM
    {
        /// <summary>EsFileTransferUpload.Id</summary>
        public Guid UploadId { get; set; }

        /// <summary>勾選的料項清單</summary>
        public List<QuotationReQuotationItemVM> Items { get; set; } = new();
    }
}
