namespace Business.DomainModel
{
    /// <summary>
    /// 各料品 Variant 資料，含客戶承認型號
    /// </summary>
    public class SanderModuleItemVariantDM : BaseDM
    {
        /// <summary>
        /// 內部料號
        /// </summary>
        public string? ItemNo { get; set; }

        /// <summary>
        /// 變異代碼（Variant Code）
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 該客戶承認之供應商型號
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 該客戶承認之供應商型號（次）
        /// </summary>
        public string? Description2 { get; set; }
    }
}
