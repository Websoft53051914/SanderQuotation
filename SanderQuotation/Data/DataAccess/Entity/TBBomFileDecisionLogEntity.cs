using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    /// <summary>
    /// BOM 決策歷程
    /// </summary>
    [Table("tb_bomfiledecisionlog")]
    public class TBBomFileDecisionLogEntity : SP_BaseEntity
    {
        /// <summary>
        /// BomFileContent.Id
        /// </summary>
        public Guid BomFileContentId { get; set; }

        /// <summary>
        /// 階段
        /// </summary>
        public int? Stage { get; set; }

        /// <summary>
        /// 步驟
        /// </summary>
        public int? Step { get; set; }

        /// <summary>
        /// 紀錄訊息
        /// </summary>
        public string? Message { get; set; }
    }
}
