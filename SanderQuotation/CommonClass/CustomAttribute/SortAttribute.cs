namespace CommonClass.CustomAttribute
{
    /// <summary>
    /// 設定 VM 與資料庫欄位的對應，用於排序功能
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SortAttribute : Attribute
    {
        /// <summary>
        /// 欄位名稱
        /// </summary>
        readonly string? columnName;

        public SortAttribute()
        {
            // 未設定預設為目標名稱
        }

        public SortAttribute(string columnName)
        {
            this.columnName = columnName;
        }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string? ColumnName
        {
            get { return columnName; }
        }
        /// <summary>
        /// 是否為預設排序欄位
        /// </summary>
        public bool IsDefault { get; set; } = false;
        /// <summary>
        /// 預設排序方向
        /// </summary>
        public string DefaultSortOrder { get; set; } = "ASC";
    }
}
