using CommonClass.CustomAttribute;
using CommonClass.Model;

namespace ViewModel
{
    /// <summary>
    /// Sander 採購型號主檔 VM
    /// </summary>
    public class SanderModuleItemVM
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 料號
        /// </summary>
        [Sort("no", IsDefault = true, DefaultSortOrder = "ASC")]
        public string? No { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        [Sort]
        public string? Description { get; set; }

        /// <summary>
        /// 品名2
        /// </summary>
        [Sort]
        public string? Description2 { get; set; }

        /// <summary>
        /// 長描述
        /// </summary>
        public string? LongDesc { get; set; }

        /// <summary>
        /// 長描述2
        /// </summary>
        public string? LongDesc2 { get; set; }
    }

    /// <summary>
    /// Sander 採購型號主檔分頁查詢條件
    /// </summary>
    public class SanderModuleItemSearchVM : ListPageEntity
    {
        /// <summary>
        /// 關鍵字篩選（料號、品名）
        /// </summary>
        public string? KeywordLike { get; set; }
    }
}
