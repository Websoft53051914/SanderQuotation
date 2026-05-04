using System;
using System.Collections.Generic;

namespace ViewModel.RecipeMgmt
{
    /// <summary>
    /// 配方清單 Grid 顯示用 VM
    /// </summary>
    public class RecipeGridVM : BaseVM
    {
        public long Id { get; set; }
        public string RecipeNo { get; set; } = "";
        public string RecipeName { get; set; } = "";
        public string CurrentVersionNo { get; set; } = "";
        public string Engineer { get; set; } = "";
        public string CreateTime { get; set; } = "";
        public string UpdateTime { get; set; } = "";

        /// <summary>版本數</summary>
        public int VersionCount { get; set; }
        /// <summary>版號規則名稱</summary>
        public string VersionNoRuleName { get; set; } = "";
    }
}
