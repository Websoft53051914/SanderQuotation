namespace Business.DomainModel
{
    public class ReportItemCustomerDM : BaseDM
    {
        /// <summary>
        /// Variant Code
        /// </summary>
        public string? VariantCode { set; get; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string? CustomerCode { set; get; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string? CustomerName { set; get; }
    }
}
