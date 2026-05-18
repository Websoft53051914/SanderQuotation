using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    /// <summary>
    /// 內部料號關鍵字資料模型
    /// </summary>
    [Table("tb_sandermoduleitemkeyword")]
    public class TBSanderModuleItemKeywordEntity : SP_BaseEntity
    {
        /// <summary>
        /// 採購型號
        /// </summary>
        [Column("no")]
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
}
