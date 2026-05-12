namespace ViewModel
{
    /// <summary>
    /// 選項項目
    /// </summary>
    public partial class SelectItemVO
    {
        public SelectItemVO()
        {

        }

        public SelectItemVO(string text, string value)
        {
            Text = text;
            Value = value;
        }

        /// <summary>
        /// 顯示文字
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    public partial class SelectItemVO
    {
        /// <summary>
        /// 是否為 BOM 檔案匯入規則
        /// </summary>
        public bool IsBomFileRule { get; set; }
    }
}
