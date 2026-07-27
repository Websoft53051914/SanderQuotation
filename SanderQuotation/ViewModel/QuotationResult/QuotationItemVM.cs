namespace ViewModel.QuotationResult
{
    /// <summary>
    /// 定時查價結果 — 單筆 BOM 料項 VM（bomfilecontent + tbbomfilequotation 聯合資料）
    /// </summary>
    public class QuotationItemVM
    {
        // ── bomfilecontent ────────────────────────────────────────────
        /// <summary>bomfilecontent 主鍵</summary>
        public Guid? Id { get; set; }

        /// <summary>元件料號</summary>
        public string? ComponentPart { get; set; }

        /// <summary>顯示用料號</summary>
        public string? DisplayPart { get; set; }

        /// <summary>元件描述</summary>
        public string? Description { get; set; }

        /// <summary>BOM 數量（允許小數）</summary>
        public decimal? Qty { get; set; }

        /// <summary>廠商</summary>
        public string? Manufacturer { get; set; }

        /// <summary>廠商型號</summary>
        public string? ManufacturerPartNumber { get; set; }

        // ── tbbomfilequotation ────────────────────────────────────────
        /// <summary>採購型號（tbbomfilequotation.no）</summary>
        public string? No { get; set; }

        /// <summary>內部採購單日期（格式化）</summary>
        public string? InternalPurchaseOrderDate { get; set; }

        /// <summary>內部單價（原幣）</summary>
        public decimal? InternalUnitPriceOriginalCurrency { get; set; }

        /// <summary>內部單價（台幣）</summary>
        public decimal? InternalUnitPriceTwd { get; set; }

        /// <summary>內部數量</summary>
        public int? InternalQuantity { get; set; }

        /// <summary>內部幣別</summary>
        public string? InternalCurrency { get; set; }

        /// <summary>內部供應商名稱</summary>
        public string? InternalSupplierName { get; set; }

        /// <summary>外部查價日期（格式化）</summary>
        public string? ExternalQuotationDate { get; set; }

        /// <summary>外部單價（原幣）</summary>
        public decimal? ExternalUnitPriceOriginalCurrency { get; set; }

        /// <summary>外部單價（台幣）</summary>
        public decimal? ExternalUnitPriceTwd { get; set; }

        /// <summary>外部最小訂購量 MOQ</summary>
        public int? ExternalMoq { get; set; }

        /// <summary>外部幣別</summary>
        public string? ExternalCurrency { get; set; }

        /// <summary>外部供應商名稱</summary>
        public string? ExternalSupplierName { get; set; }

        /// <summary>是否為建議採購型號</summary>
        public bool IsRecommendedNo { get; set; }

        /// <summary>比對結果分類（1=完全命中 2=建議料號 3=未命中）</summary>
        public int? MatchCategory { get; set; }

        /// <summary>比對命中欄位（MPN / Component Part / 規格）</summary>
        public string? MatchField { get; set; }

        /// <summary>比對結果分類（文字）</summary>
        public string? MatchCategoryText { get; set; }

        /// <summary>內部供應商代碼</summary>
        public string? InternalSupplierCode { get; set; }

        /// <summary>採購型號 Description_2</summary>
        public string? InternalItemDescription2 { get; set; }

        /// <summary>外部查價庫存量</summary>
        public int? ExternalStock { get; set; }

        /// <summary>外部查價情境（1=情境A優先名單 2=情境B後備）</summary>
        public int? ExternalScenario { get; set; }

        /// <summary>外部查價情境（文字，對應 ExternalScenarioEnum Description）</summary>
        public string? ExternalScenarioText { get; set; }

        // ── tbbomfilequotationother (Mouser) ──────────────────────────
        /// <summary>Mouser 查價日期（格式化）</summary>
        public string? MouserQuotationDate { get; set; }

        /// <summary>Mouser 單價（原幣）</summary>
        public decimal? MouserUnitPriceOriginalCurrency { get; set; }

        /// <summary>Mouser 單價（台幣）</summary>
        public decimal? MouserUnitPriceTwd { get; set; }

        /// <summary>Mouser MOQ</summary>
        public int? MouserMoq { get; set; }

        /// <summary>Mouser 幣別</summary>
        public string? MouserCurrency { get; set; }

        /// <summary>Mouser 供應商名稱</summary>
        public string? MouserSupplierName { get; set; }

        // ── tbbomfilequotationother (DigiKey) ─────────────────────────
        /// <summary>DigiKey 查價日期（格式化）</summary>
        public string? DkQuotationDate { get; set; }

        /// <summary>DigiKey 單價（原幣）</summary>
        public decimal? DkUnitPriceOriginalCurrency { get; set; }

        /// <summary>DigiKey 單價（台幣）</summary>
        public decimal? DkUnitPriceTwd { get; set; }

        /// <summary>DigiKey MOQ</summary>
        public int? DkMoq { get; set; }

        /// <summary>DigiKey 幣別</summary>
        public string? DkCurrency { get; set; }

        /// <summary>DigiKey 供應商名稱</summary>
        public string? DkSupplierName { get; set; }

        /// <summary>是否套用 Variant 客戶承認料過濾（文字：是/否）</summary>
        public string? IsFilterByCustomerApprovedPartText { get; set; }
    }
}
