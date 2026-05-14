namespace ViewModel.TableExcel
{
    /// <summary>
    /// 資料表轉檔對應設定 Grid 列表 ViewModel
    /// </summary>
    public class TableExcelGridVM
    {
        public int No { get; set; }

        public Guid RowGuid { get; set; }

        public string TransferMappingCode { get; set; } = "";

        /// <summary>匯入範本檔案名稱</summary>
        public string ExampleFileName { get; set; } = "";

        /// <summary>匯入範本檔案類型（文字顯示，例如 xlsx / xls / csv）</summary>
        public string ExampleFileTypeText { get; set; } = "";

        /// <summary>NAS 檔案路徑</summary>
        public string SrcNasFilePath { get; set; } = "";

        /// <summary>備註</summary>
        public string? Description { get; set; }

        /// <summary>對應資料表（逗號分隔）</summary>
        public string? MappingTables { get; set; }

        public string Status { get; set; } = "";

        public bool CanDelete { get; set; }
    }
}
