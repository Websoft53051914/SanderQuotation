using System;

namespace ViewModel.RecipeMgmt
{
    /// <summary>
    /// 配方新增 / 編輯 Form VM
    /// </summary>
    public class RecipeEditVM : BaseVM
    {
        public long Id { get; set; }
        public string RecipeNo { get; set; } = "";
        public string RecipeName { get; set; } = "";
        public string CurrentVersionNo { get; set; } = "";
        public string Engineer { get; set; } = "";
        public string CreateTime { get; set; } = "";
        public string UpdateTime { get; set; } = "";
        public string RecipeContent { get; set; } = "";
        public long Creator { get; set; }
        public long Updater { get; set; }

        /// <summary>狀態代碼：0=草稿, 1=已發行, 2=審核中, 9=已廢止</summary>
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = "";
        /// <summary>關聯版號規則 ID（0 = 無規則）</summary>
        public long VersionNoRuleId { get; set; }
        /// <summary>版號規則名稱（展示用）</summary>
        public string VersionNoRuleName { get; set; } = "";
    }
}
