namespace Data.DataAccess.DTO
{
    /// <summary>
    /// 依客戶料號在規格欄位字面比對的結果
    /// </summary>
    public class SanderModuleItemPartMatchDTO
    {
        /// <summary>內部料號</summary>
        public string? No { get; set; }

        /// <summary>命中欄位（LongDesc / LongDesc2 / Description2）</summary>
        public string? MatchedField { get; set; }
    }
}
