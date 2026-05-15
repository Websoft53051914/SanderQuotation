namespace Business.DomainModel
{
    /// <summary>
    /// BOM 決策歷程
    /// </summary>
    public class TBBomFileDecisionLogDM : BaseDM
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
