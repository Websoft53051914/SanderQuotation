namespace Const.ApiModels.LEDCCDevice
{
    public class SetDefaultDisplayReqVO
    {   
        /// <summary>
        /// IP
        /// </summary>
        public string IP { get; set; } = string.Empty;
        /// <summary>
        /// 預設歡迎文字
        /// </summary>
        public string DefaultWelcomeText { get; set; } = string.Empty;
        /// <summary>
        /// 社區名稱
        /// </summary>
        public string CommunityName { get; set; } = string.Empty;
        /// <summary>
        /// 時間顯示項(以逗號隔開)
        /// </summary>
        public string TimeDisplayItems { get; set; } = "";
    }
}
