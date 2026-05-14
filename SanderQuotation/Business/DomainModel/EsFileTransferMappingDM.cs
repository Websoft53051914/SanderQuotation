namespace Business.DomainModel
{
    public class EsFileTransferMappingDM //: BaseDM
    {
        public Guid Id { get; set; }

        public int Status { get; set; }

        /// <summary>
        /// 蝟餌絞蝺刻? (?芸??Ｙ?)
        /// </summary>
        public string TransferMappingCode { set; get; }

        /// <summary>
        /// ?臬蝭瑼??迂
        /// </summary>
        public string ExampleFileName { set; get; }

        /// <summary>
        /// ?臬蝭瑼?憿?
        /// </summary>
        public int ExampleFileType { set; get; }

        /// <summary>
        /// NAS 瑼?頝臬?
        /// </summary>
        public string SrcNasFilePath { set; get; }

        /// <summary>
        /// ?酉
        /// </summary>
        public string? Description { set; get; }

        /// <summary>
        /// 撖阡?瑼??迂
        /// </summary>
        public string? FileName { set; get; }

        /// <summary>
        /// 撠?鞈?銵剁?????嚗 EsFileTransferMappingColumn JOIN ??嚗?
        /// </summary>
        public string? MappingTables { get; set; }

        /// <summary>
        /// 甈?撠?皜
        /// </summary>
        public List<EsFileTransferMappingColumnDM> Columns { set; get; } = new();

        /// <summary>
        /// 是否為 BOM 檔案規則（EsFileTransferMappingColumn 下有 TargetTableName = 'bomfilecontent'）
        /// </summary>
        public bool IsBomFileRule { get; set; }

        //額外欄位
        public List<EsScheduleCycleDM> EsScheduleCycleDMs { get; set; } = new();
    }
}
