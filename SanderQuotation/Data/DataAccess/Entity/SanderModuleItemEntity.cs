using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    /// <summary>
    /// ERP 料號基本資料
    /// </summary>
    [Table("sandermoduleitem")]
    public class SanderModuleItemEntity : Core.Utility.Base.Data.GuidId.BaseEntity
    {
        /// <summary>
        /// 採購型號
        /// </summary>
        [Column("no")]
        public string? No { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 描述 2
        /// </summary>
        public string? Description2 { get; set; }

        /// <summary>
        /// 長描述
        /// </summary>
        public string? LongDesc { get; set; }

        /// <summary>
        /// 長描述 2
        /// </summary>
        public string? LongDesc2 { get; set; }
    }
}
