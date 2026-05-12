using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("reportitemcustomer")]
    public class ReportItemCustomerEntity : Core.Utility.Base.Data.GuidId.BaseEntity
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
