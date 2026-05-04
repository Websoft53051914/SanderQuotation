namespace Business.DomainModel
{
    /// <summary>
    /// SOP 工單 DM（下拉選單用）
    /// </summary>
    public class SopOrderDM
    {
        /// <summary>工單 ID</summary>
        public string OrderID { get; set; }

        /// <summary>顯示文字（文件編號 + 版本）</summary>
        public string DisplayText { get; set; }
    }

    /// <summary>
    /// SOP 站點 DM（下拉選單用）
    /// </summary>
    public class SopStationDM
    {
        /// <summary>工站編號</summary>
        public string OpNo { get; set; }

        /// <summary>工站名稱</summary>
        public string OpName { get; set; }
    }

    /// <summary>
    /// SOP 工規規則列DM
    /// </summary>
    public class SopRuleRowDM
    {
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
    /// SOP 工規備註 DM
    /// </summary>
    public class SopRemarkDM
    {
        /// <summary>備註內容</summary>
        public string ContentText { get; set; }
    }

    /// <summary>
    /// SOP 站點工規內容 DM
    /// </summary>
    public class SopStationContentDM
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
        public List<SopRuleRowDM> RuleList { get; set; }

        /// <summary>備註列表</summary>
        public List<SopRemarkDM> Remark { get; set; }

        /// <summary>圖片路徑列表</summary>
        public List<string> ImageFilePaths { get; set; }
    }
}
