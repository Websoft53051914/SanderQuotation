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

        /// <summary>元件描述</summary>
        public string? Description { get; set; }

        /// <summary>BOM 數量</summary>
        public int? Qty { get; set; }

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
    }
}
