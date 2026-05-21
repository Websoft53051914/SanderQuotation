namespace Business.DomainModel
{
    /// <summary>
    /// BOM 現貨優惠價結果
    /// </summary>
    public class TBBomFileQuotationOtherDM : BaseDM
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
        /// BomFileContent.Id
        /// </summary>
        public Guid BomFileContentId { get; set; }

        /// <summary>
        /// 來源類型
        /// </summary>
        public int SourceType { get; set; }
    }
}
