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
    /// 重新內部查價 — 請求 VM
    /// </summary>
    public class QuotationReInternalQuotationRequestVM
    {
        /// <summary>bomfilecontent.Id</summary>
        public Guid BomFileContentId { get; set; }

        /// <summary>前端調整後的採購型號</summary>
        public string? No { get; set; }
    }

    /// <summary>
    /// 重新外部查價 — 請求 VM
    /// </summary>
    public class QuotationReExternalQuotationRequestVM
    {
        /// <summary>bomfilecontent.Id</summary>
        public Guid BomFileContentId { get; set; }
    }

    /// <summary>
    /// 查詢現貨優惠價 — 請求 VM
    /// </summary>
    public class QuotationCheckInStockPriceRequestVM
    {
        /// <summary>bomfilecontent.Id</summary>
        public Guid BomFileContentId { get; set; }
    }
}
