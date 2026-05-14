namespace Business.DomainModel
{
    /// <summary>
    /// 內部料品表
    /// </summary>
    public class SanderModuleItemDM : BaseDM
    {
        /// <summary>
        /// 內部料號
        /// </summary>
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
