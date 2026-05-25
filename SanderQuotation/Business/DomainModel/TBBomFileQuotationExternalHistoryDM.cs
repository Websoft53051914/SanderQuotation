namespace Business.DomainModel
{
    /// <summary>
    /// BOM 外部查價歷史
    /// </summary>
    public class TBBomFileQuotationExternalHistoryDM : BaseDM
    {
        /// <summary>
        /// 查價日期
        /// </summary>
        public DateTime? QuotationDate { get; set; }

        /// <summary>
        /// 單價(原幣)
        /// </summary>
        public decimal? UnitPriceOriginalCurrency { get; set; }

        /// <summary>
        /// 單價(台幣)
        /// </summary>
        public decimal? UnitPriceTwd { get; set; }

        /// <summary>
        /// 最小訂購量(MOQ)
        /// </summary>
        public int? Moq { get; set; }

        /// <summary>
        /// 幣別
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// 供應商名稱
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// 庫存量
        /// </summary>
        public int? Stock { get; set; }

        /// <summary>
        /// 廠商型號
        /// </summary>
        public string? ManufacturerPartNumber { get; set; }
    }
}
