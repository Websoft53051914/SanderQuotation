using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    /// <summary>
    /// 系統設定
    /// </summary>
    [Table("tb_syssetting")]
    public class TBSysSettingEntity : SP_BaseEntity
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
