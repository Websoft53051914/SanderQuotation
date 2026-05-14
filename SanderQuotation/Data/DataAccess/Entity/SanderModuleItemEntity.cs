using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    /// <summary>
    /// 內部料品表
    /// </summary>
    [Table("sandermoduleitem")]
    public class SanderModuleItemEntity : Core.Utility.Base.Data.GuidId.BaseEntity
    {
        /// <summary>
        /// 內部料號
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

        /// <summary>
        /// 判斷是否需比對廠牌之類別代碼
        /// </summary>
        public string? ItemCategoryCode { get; set; }
    }
}
