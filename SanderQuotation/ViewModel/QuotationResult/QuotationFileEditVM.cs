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

        /// <summary>是否顯示 AI 決策過程（AI 決策過程顯示開關）</summary>
        public bool IsAIDecisionProcessDisplay { get; set; } = true;
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

    /// <summary>
    /// 料號快查 — 請求 VM
    /// </summary>
    public class QuotationQuickSearchRequestVM
    {
        /// <summary>製造商料號（必填）</summary>
        public string ManufacturerPartNumber { get; set; } = string.Empty;

        /// <summary>元件料號（選填）</summary>
        public string? ComponentPart { get; set; }

        /// <summary>製造商（選填）</summary>
        public string? Manufacturer { get; set; }

        /// <summary>客戶代碼（選填）</summary>
        public string? CustomerCode { get; set; }

        /// <summary>報價數量</summary>
        public int PurchaseQty { get; set; } = 1;
    }

    /// <summary>
    /// 快查 — 查料請求 VM（至少填一欄）
    /// </summary>
    public class QuotationQuickPartMatchRequestVM
    {
        /// <summary>製造商料號（選填，至少四欄之一必填）</summary>
        public string? ManufacturerPartNumber { get; set; }

        /// <summary>製造商（選填）</summary>
        public string? Manufacturer { get; set; }

        /// <summary>元件料號（選填）</summary>
        public string? ComponentPart { get; set; }

        /// <summary>元件描述（選填）</summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 快查 — 查價請求 VM
    /// </summary>
    public class QuotationQuickPricingRequestVM
    {
        /// <summary>製造商料號（外部查價/現貨查價 必填）</summary>
        public string? ManufacturerPartNumber { get; set; }

        /// <summary>採購型號（內部查價 必填）</summary>
        public string? No { get; set; }

        /// <summary>客戶代碼（內部查價 選填）</summary>
        public string? CustomerCode { get; set; }

        /// <summary>查價數量</summary>
        public int PurchaseQty { get; set; } = 1;

        /// <summary>是否執行內部查價</summary>
        public bool RunInternal { get; set; }

        /// <summary>是否執行外部查價</summary>
        public bool RunExternal { get; set; }

        /// <summary>是否執行現貨/DigiKey 查價</summary>
        public bool RunInStock { get; set; }
    }
}
