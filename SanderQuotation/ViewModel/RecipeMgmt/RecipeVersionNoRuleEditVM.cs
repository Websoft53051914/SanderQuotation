namespace ViewModel.RecipeMgmt
{
    /// <summary>
    /// 版號規則新增 / 編輯 VM（對應 TB_RecipeVersionNoRule 單筆維護）
    /// </summary>
    public class RecipeVersionNoRuleEditVM : BaseVM
    {
        /// <summary>主鍵（新增時傳 0）</summary>
        public long Id { get; set; }

        /// <summary>規則名稱</summary>
        public string RuleName { get; set; } = "";

        /// <summary>
        /// 區段設定 JSON 字串（序列化自 <c>RuleSettingData</c>）。
        /// <br/>格式範例：{"Segments":[{"Order":1,"Type":"FIXED","Options":{"Text":"V"}},…]}
        /// </summary>
        public string RuleSetting { get; set; } = "";

        /// <summary>狀態碼（0=草稿, 1=啟用, 2=停用）</summary>
        public int StatusCode { get; set; }
    }
}
