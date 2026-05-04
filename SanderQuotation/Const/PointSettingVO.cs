using System.Text.Json.Serialization;

namespace Const
{
    /// <summary>
    /// 點位設定資料模型
    /// </summary>
    public class PointSettingVO
    {
        /// <summary>
        /// 點位(0-based)
        /// </summary>
        public int PointNo { get; set; }

        /// <summary>
        /// 點位名稱
        /// </summary>
        public string? PointName { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 是否已選取
        /// </summary>
        public bool IsSelected { get; set; } = false;
    }
}
