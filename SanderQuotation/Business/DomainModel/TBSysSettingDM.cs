namespace Business.DomainModel
{
    /// <summary>
    /// 系統設定
    /// </summary>
    public class TBSysSettingDM : BaseDM
    {
        /// <summary>
        /// 參數名稱
        /// </summary>
        public string Param { get; set; } = string.Empty;

        /// <summary>
        /// 參數值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 類型
        /// </summary>
        public string Type { get; set; } = string.Empty;
    }
}
