namespace Data.Common.SopDb.DTO
{
    /// <summary>
    /// SOP 工單下拉選單 DTO
    /// </summary>
    public class SopOrderDTO
    {
        /// <summary>工單 ID</summary>
        public string OrderID { get; set; }

        /// <summary>文件編號</summary>
        public string DocNo { get; set; }

        /// <summary>文件版本</summary>
        public string DocVersion { get; set; }

        /// <summary>規格頁 JSON 資料（包含站點/機台/參數等）</summary>
        public string ItemPageData { get; set; }
    }
}
