using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    /// <summary>
    /// 各料品 Variant 資料，含客戶承認型號
    /// </summary>
    [Table("sandermoduleitemvariant")]
    public class SanderModuleItemVariantEntity : Core.Utility.Base.Data.GuidId.BaseEntity
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

        /// <summary>
        /// 是否需要執行 AI 關鍵字抽取（0 = 否，直接使用 CustomerApprovedPartCsv；非 0 = 是）
        /// </summary>
        public int FlagNeedExtractKeyword { get; set; }

        /// <summary>
        /// 客戶承認料（多筆以「,」隔開）
        /// </summary>
        public string? CustomerApprovedPartCsv { get; set; }
    }
}
