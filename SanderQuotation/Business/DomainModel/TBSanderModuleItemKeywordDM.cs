namespace Business.DomainModel
{
    /// <summary>
    /// 內部料號關鍵字資料模型
    /// </summary>
    public partial class TBSanderModuleItemKeywordDM : BaseDM
    {
        /// <summary>
        /// 採購型號
        /// </summary>
        public string? No { get; set; }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// 關鍵字
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 向量資料 (embedding)
        /// </summary>
        public float[]? KeywordEmbedding { get; set; }
    }

    public partial class TBSanderModuleItemKeywordDM
    {
        /// <summary>
        /// 相似度
        /// </summary>
        public decimal? SimilarityScore { get; set; }
    }
}
