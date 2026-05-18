namespace Business.DomainModel
{
    /// <summary>
    /// 內部採購紀錄
    /// </summary>
    public class SanderModulePurchaseLineDM : BaseDM
    {
        /// <summary>
        /// 單據日期
        /// </summary>
        public DateTime? DocumentDate { get; set; }

        /// <summary>
        /// 內部料號
        /// </summary>
        public string? No { get; set; }

        /// <summary>
        /// 供應商代碼
        /// </summary>
        public string? BuyFromVendorNo { get; set; }

        /// <summary>
        /// 供應商名稱
        /// </summary>
        public string? BuyFromVendorName { get; set; }

        /// <summary>
        /// 單價（原幣）
        /// </summary>
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// 單價（本幣）
        /// </summary>
        public decimal? UnitCostLcy { get; set; }

        /// <summary>
        /// 採購數量
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// 幣別代碼
        /// </summary>
        public string? CurrencyCode { get; set; }

        /// <summary>
        /// 採購當下之供應商型號
        /// </summary>
        public string? Description2 { get; set; }
    }
}
