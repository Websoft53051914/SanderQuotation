using Data.DataAccess.Entity;

namespace Data.DataAccess.DTO
{
    /// <summary>
    /// 內部料號關鍵字資料模型 DTO
    /// </summary>
    public class TBSanderModuleItemKeywordDTO : TBSanderModuleItemKeywordEntity
    {
        /// <summary>
        /// 相似度（查詢時計算）
        /// </summary>
        public decimal? SimilarityScore { get; set; }
    }
}
