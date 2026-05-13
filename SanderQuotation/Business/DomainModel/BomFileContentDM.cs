namespace Business.DomainModel
{
    /// <summary>
    /// BOM 表內容
    /// </summary>
    public class BomFileContentDM : BaseDM
    {
        /// <summary>
        /// 元件料號
        /// </summary>
        public string? ComponentPart { get; set; }

        /// <summary>
        /// 元件描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        public int? Qty { get; set; }

        /// <summary>
        /// 廠商
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// 廠商型號
        /// </summary>
        public string? ManufacturerPartNumber { get; set; }

        /// <summary>
        /// 顯示用料號
        /// </summary>
        public string? DisplayPart { get; set; }

        /// <summary>
        /// 檔案儲存代號(系統使用)
        /// </summary>
        public Guid UploadId { get; set; }
    }
}
