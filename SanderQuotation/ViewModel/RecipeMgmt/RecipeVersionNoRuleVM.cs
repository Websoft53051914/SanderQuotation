namespace ViewModel.RecipeMgmt
{
    /// <summary>
    /// 版號規則清單 Grid VM（對應 TB_RecipeVersionNoRule 清單顯示）
    /// </summary>
    public class RecipeVersionNoRuleVM : BaseVM
    {
        /// <summary>規則主鍵</summary>
        public long Id { get; set; }

        /// <summary>規則名稱</summary>
        public string RuleName { get; set; } = "";

        /// <summary>狀態碼（0=草稿, 1=啟用, 2=停用, 9=已廢止）</summary>
        public int StatusCode { get; set; }

        /// <summary>狀態顯示名稱</summary>
        public string StatusName { get; set; } = "";

        /// <summary>狀態 Badge CSS class</summary>
        public string StatusCss { get; set; } = "";

        /// <summary>版號預覽（以初始種子模擬第一個版本，例如 "V1.0.0"）</summary>
        public string PreviewVersion { get; set; } = "";

        /// <summary>建立日期（格式化字串）</summary>
        public string CreateTime { get; set; } = "";

        /// <summary>最後更新時間（格式化字串）</summary>
        public string UpdateTime { get; set; } = "";
    }
}
