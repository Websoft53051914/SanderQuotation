namespace Business.DomainModel
{
    public class EsFileTransferMappingDM //: BaseDM
    {
        public Guid Id { get; set; }

        public string Status { get; set; }

        /// <summary>
        /// ç³»çµ±ç·¨è? (?ªå??¢ç?)
        /// </summary>
        public string TransferMappingCode { set; get; }

        /// <summary>
        /// ?¯å…¥ç¯„æœ¬æª”æ??ç¨±
        /// </summary>
        public string ExampleFileName { set; get; }

        /// <summary>
        /// ?¯å…¥ç¯„æœ¬æª”æ?é¡å?
        /// </summary>
        public int ExampleFileType { set; get; }

        /// <summary>
        /// NAS æª”æ?è·¯å?
        /// </summary>
        public string SrcNasFilePath { set; get; }

        /// <summary>
        /// ?™è¨»
        /// </summary>
        public string? Description { set; get; }

        /// <summary>
        /// å¯¦é?æª”æ??ç¨±
        /// </summary>
        public string? FileName { set; get; }

        /// <summary>
        /// å°æ?è³‡æ?è¡¨ï??—è??†é?ï¼Œç”± EsFileTransferMappingColumn JOIN ?–å?ï¼?
        /// </summary>
        public string? MappingTables { get; set; }

        /// <summary>
        /// æ¬„ä?å°æ?æ¸…å–®
        /// </summary>
        public List<EsFileTransferMappingColumnDM> Columns { set; get; } = new();
    }
}
