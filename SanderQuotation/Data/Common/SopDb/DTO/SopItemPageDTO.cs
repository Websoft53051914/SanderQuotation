namespace Data.Common.SopDb.DTO
{
    /// <summary>
    /// SOP 工規規則欄位 DTO（對應 ItemPageRule JSON）
    /// </summary>
    public class SopRuleRowDTO
    {
        /// <summary>工站編號</summary>
        public string OpNo { get; set; }

        /// <summary>規則類型</summary>
        public string RuleType { get; set; }

        /// <summary>機台</summary>
        public string Machine { get; set; }

        /// <summary>檢驗項目</summary>
        public string Item { get; set; }

        /// <summary>規格</summary>
        public string Spec { get; set; }

        /// <summary>參考值</summary>
        public string Reference { get; set; }
    }

    /// <summary>
    /// SOP 工規備註 DTO（對應 ItemPageRemark JSON）
    /// </summary>
    public class SopRemarkDTO
    {
        /// <summary>工站編號</summary>
        public string OpNo { get; set; }

        /// <summary>規則類型</summary>
        public string RuleType { get; set; }

        /// <summary>備註內容</summary>
        public string ContentText { get; set; }
    }

    /// <summary>
    /// SOP 工規站點內容 DTO（對應 ItemPage JSON）
    /// </summary>
    public class SopItemPageDTO
    {
        /// <summary>工站編號</summary>
        public string OpNo { get; set; }

        /// <summary>工站名稱</summary>
        public string OpName { get; set; }

        /// <summary>特殊說明</summary>
        public string Special { get; set; }

        /// <summary>規則類型</summary>
        public string RuleType { get; set; }

        /// <summary>工規規則列表</summary>
        public List<SopRuleRowDTO> RuleList { get; set; }

        /// <summary>備註列表</summary>
        public List<SopRemarkDTO> Remark { get; set; }

        /// <summary>圖片檔案路徑列表</summary>
        public List<string> ImageFilePathList { get; set; }
    }
}
