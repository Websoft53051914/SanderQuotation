namespace ViewModel.SanderModule
{
    /// <summary>
    /// Sander 採購型號主檔 VM
    /// </summary>
    public class SanderModuleItemVM
    {
        /// <summary>主鍵</summary>
        public long Id { get; set; }

        /// <summary>料號</summary>
        public string No { get; set; } = "";

        /// <summary>品名</summary>
        public string Description { get; set; } = "";

        /// <summary>品名2</summary>
        public string Description2 { get; set; } = "";

        /// <summary>長描述</summary>
        public string LongDesc { get; set; } = "";

        /// <summary>長描述2</summary>
        public string LongDesc2 { get; set; } = "";
    }
}
