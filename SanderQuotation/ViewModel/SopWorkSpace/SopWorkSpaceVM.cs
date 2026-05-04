namespace ViewModel.SopWorkSpace
{
    /// <summary>
    /// SOP 工單選單 VM
    /// </summary>
    public class SopOrderVM
    {
        /// <summary>工單 ID</summary>
        public string OrderID { get; set; }

        /// <summary>顯示文字</summary>
        public string DisplayText { get; set; }
    }

    /// <summary>
    /// SOP 站點選單 VM
    /// </summary>
    public class SopStationVM
    {
        /// <summary>工站編號</summary>
        public string OpNo { get; set; }

        /// <summary>工站名稱</summary>
        public string OpName { get; set; }
    }

    /// <summary>
    /// SOP 工規規則列 VM
    /// </summary>
    public class SopRuleRowVM
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
    /// SOP 備註 VM
    /// </summary>
    public class SopRemarkVM
    {
        /// <summary>備註內容</summary>
        public string ContentText { get; set; }
    }

    /// <summary>
    /// SOP 站點工規內容 VM（API 回傳給前端）
    /// </summary>
    public class SopStationContentVM
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
        public List<SopRuleRowVM> RuleList { get; set; }

        /// <summary>備註列表</summary>
        public List<SopRemarkVM> Remark { get; set; }

        /// <summary>圖片路徑列表</summary>
        public List<string> ImageFilePaths { get; set; }
    }
}
