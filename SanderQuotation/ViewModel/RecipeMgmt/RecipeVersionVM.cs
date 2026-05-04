using System;

namespace ViewModel.RecipeMgmt
{
    /// <summary>
    /// 配方版本歷程 VM
    /// </summary>
    public class RecipeVersionVM : BaseVM
    {
        public long Id { get; set; }
        public long RecipeId { get; set; }
        public string VersionNo { get; set; } = "";
        public string VersionLog { get; set; } = "";
        public string RecipeContent { get; set; } = "";
        public bool IsCurrent { get; set; }

        /// <summary>版號異動種子（其他用途保留）</summary>
        public string VersionSeed { get; set; } = "";
        public string VersionSeedCss { get; set; } = "";

        /// <summary>版號異動類型（TB_RecipeVersion.VersionBumpType）：MAJOR / MINOR / PATCH</summary>
        public string VersionBumpType { get; set; } = "";
        public string VersionBumpTypeCss { get; set; } = "";

        public string CreateTime { get; set; } = "";
        public string CreatorName { get; set; } = "";
        public string UpdaterName { get; set; } = "";
    }
}
