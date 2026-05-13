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
        public int? InternalCurrency { get; set; }

        /// <summary>
        /// 內部供應商名稱
        /// </summary>
        public int? InternalSupplierName { get; set; }

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
        public int? ExternalCurrency { get; set; }

        /// <summary>
        /// 外部供應商名稱
        /// </summary>
        public int? ExternalSupplierName { get; set; }

        /// <summary>
        /// BomFileContent.Id
        /// </summary>
        public Guid BomFileContentId { get; set; }
    }
}
