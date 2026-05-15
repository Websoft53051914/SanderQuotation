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
        /// 料品規格主欄位
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 料品規格次欄位
        /// </summary>
        public string? Description2 { get; set; }

        /// <summary>
        /// MPN 主要欄位
        /// </summary>
        public string? LongDesc { get; set; }

        /// <summary>
        /// MPN 次要欄位＋自由文字備註
        /// </summary>
        public string? LongDesc2 { get; set; }

        /// <summary>
        /// 判斷是否需比對廠牌之類別代碼
        /// </summary>
        public string? ItemCategoryCode { get; set; }

        /// <summary>
        /// 是否需要執行 AI 關鍵字抽取
        /// </summary>
        public bool FlagNeedExtractKeyword { get; set; }
    }
}
