namespace Business.DomainModel
{
    /// <summary>
    /// BOM 查價(料)結果
    /// </summary>
    public partial class TBBomFileQuotationDM : BaseDM
    {
        /// <summary>
        /// 採購型號
        /// </summary>
        public string? No { get; set; }

        /// <summary>
        /// 內部採購單日期
        /// </summary>
        public DateTime? InternalPurchaseOrderDate { get; set; }

        /// <summary>
        /// 內部單價(原幣)
        /// </summary>
        public decimal? InternalUnitPriceOriginalCurrency { get; set; }

        /// <summary>
        /// 內部單價(台幣)
        /// </summary>
        public decimal? InternalUnitPriceTwd { get; set; }

        /// <summary>
        /// 內部數量
        /// </summary>
        public int? InternalQuantity { get; set; }

        /// <summary>
        /// 內部幣別
        /// </summary>
        public string? InternalCurrency { get; set; }

        /// <summary>
        /// 內部供應商名稱
        /// </summary>
        public string? InternalSupplierName { get; set; }

        /// <summary>
        /// 外部查價日期
        /// </summary>
        public DateTime? ExternalQuotationDate { get; set; }

        /// <summary>
        /// 外部單價(原幣)
        /// </summary>
        public decimal? ExternalUnitPriceOriginalCurrency { get; set; }

        /// <summary>
        /// 外部單價(台幣)
        /// </summary>
        public decimal? ExternalUnitPriceTwd { get; set; }

        /// <summary>
        /// 外部最小訂購量(MOQ)
        /// </summary>
        public int? ExternalMoq { get; set; }

        /// <summary>
        /// 外部幣別
        /// </summary>
        public string? ExternalCurrency { get; set; }

        /// <summary>
        /// 外部供應商名稱
        /// </summary>
        public string? ExternalSupplierName { get; set; }

        /// <summary>
        /// BomFileContent.Id
        /// </summary>
        public Guid BomFileContentId { get; set; }

        /// <summary>
        /// 是否為建議採購型號
        /// </summary>
        public bool IsRecommendedNo { get; set; }

        /// <summary>
        /// 內部查價 AI 分群低價群最低價
        /// </summary>
        public decimal? InternalLowMinPrice { get; set; }

        /// <summary>
        /// 內部查價 AI 分群低價群最高價
        /// </summary>
        public decimal? InternalLowMaxPrice { get; set; }

        /// <summary>
        /// 內部查價 AI 分群高價群最低價
        /// </summary>
        public decimal? InternalHighMinPrice { get; set; }

        /// <summary>
        /// 內部查價 AI 分群高價群最高價
        /// </summary>
        public decimal? InternalHighMaxPrice { get; set; }

        /// <summary>
        /// 是否套用 Variant 客戶承認料過濾
        /// </summary>
        public bool IsFilterByCustomerApprovedPart { get; set; }

        /// <summary>
        /// 內部查價所使用的客戶承認料清單（CSV 格式）
        /// </summary>
        public string? CustomerApprovedPartCsv { get; set; }

        /// <summary>
        /// 比對結果分類（對應 MatchCategoryEnum：1=完全命中 2=建議料號 3=未命中）
        /// </summary>
        public int? MatchCategory { get; set; }

        /// <summary>
        /// 比對命中欄位（MPN / Component Part / 規格）
        /// </summary>
        public string? MatchField { get; set; }

        /// <summary>
        /// 內部供應商代碼
        /// </summary>
        public string? InternalSupplierCode { get; set; }

        /// <summary>
        /// 採購型號 Description_2
        /// </summary>
        public string? InternalItemDescription2 { get; set; }

        /// <summary>
        /// 外部查價庫存量
        /// </summary>
        public int? ExternalStock { get; set; }

        /// <summary>
        /// 外部查價情境（對應 ExternalScenarioEnum：1=情境A優先名單 2=情境B後備）
        /// </summary>
        public int? ExternalScenario { get; set; }
    }
}
