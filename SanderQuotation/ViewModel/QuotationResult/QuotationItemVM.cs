namespace ViewModel.QuotationResult
{
    /// <summary>
    /// 定時查價結果 — 單筆 BOM 料項 VM（含內部採購紀錄與外部查價）
    /// </summary>
    public class QuotationItemVM
    {
        /// <summary>主鍵</summary>
        public long Id { get; set; }

        /// <summary>所屬查價檔案 ID</summary>
        public long FileId { get; set; }

        // ── BOM 基本資料 ──────────────────────────────────────────────
        /// <summary>元件描述</summary>
        public string Description { get; set; } = "";

        /// <summary>製造商1</summary>
        public string Manufacturer1 { get; set; } = "";

        /// <summary>製造商料號1</summary>
        public string ManufacturerPartNo1 { get; set; } = "";

        /// <summary>BOM 數量</summary>
        public int Quantity { get; set; }

        // ── 內部採購紀錄 ──────────────────────────────────────────────
        /// <summary>採購日期</summary>
        public string InternalProcurementDate { get; set; } = "";

        /// <summary>內部採購單價（原幣）</summary>
        public decimal? InternalUnitPriceOrig { get; set; }

        /// <summary>內部採購單價（台幣）</summary>
        public decimal? InternalUnitPriceTwd { get; set; }

        /// <summary>內部採購數量</summary>
        public int? InternalQty { get; set; }

        /// <summary>內部採購幣別</summary>
        public string InternalCurrency { get; set; } = "";

        /// <summary>內部供應商名稱</summary>
        public string InternalSupplierName { get; set; } = "";

        // ── 外部查價 ──────────────────────────────────────────────────
        /// <summary>查價日期</summary>
        public string ExternalQuotationDate { get; set; } = "";

        /// <summary>外部查價單價（原幣）</summary>
        public decimal? ExternalUnitPriceOrig { get; set; }

        /// <summary>外部查價單價（台幣）</summary>
        public decimal? ExternalUnitPriceTwd { get; set; }

        /// <summary>最小訂購量 MOQ</summary>
        public int? ExternalMOQ { get; set; }

        /// <summary>外部查價幣別</summary>
        public string ExternalCurrency { get; set; } = "";

        /// <summary>外部供應商名稱</summary>
        public string ExternalSupplierName { get; set; } = "";

        // ── 可修改欄位 ────────────────────────────────────────────────
        /// <summary>採購型號（可修改）</summary>
        public string ProcurementModel { get; set; } = "";

        /// <summary>是否為建議採購型號（建議項目不會有內部採購紀錄）</summary>
        public bool IsRecommended { get; set; }

        /// <summary>可選的採購型號清單（用於 select2 下拉）</summary>
        public List<string> ProcurementModelOptions { get; set; } = new();
    }
}
