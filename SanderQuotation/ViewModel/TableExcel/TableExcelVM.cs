namespace ViewModel.TableExcel
{
    /// <summary>
    /// Excel 上傳後每個工作表的解析結果
    /// </summary>
    public class ExcelSheetInfoVM
    {
        public string Name { get; set; } = "";
        public int ColumnCount { get; set; }
        public List<string> Columns { get; set; } = new();
    }

    /// <summary>
    /// Excel 上傳 API 回傳結果
    /// </summary>
    public class UploadExcelResultVM
    {
        public string FilePath { get; set; } = "";
        public List<ExcelSheetInfoVM> Sheets { get; set; } = new();
    }

    /// <summary>
    /// 資料表轉檔對應設定主檔
    /// </summary>
    public class TableExcelSettingVM
    {
        public Guid RowGuid { get; set; }
        public int No { get; set; }
        /// <summary>匯入範本檔案名稱</summary>
        public string ExampleFileName { get; set; } = "";
        /// <summary>匯入範本檔案類型</summary>
        public string ExampleFileType { get; set; } = "";
        /// <summary>NAS 檔案路徑</summary>
        public string SrcNasFilePath { get; set; } = "";
        /// <summary>備註</summary>
        public string? Description { get; set; }
        /// <summary>上傳的暫存檔案路徑（伺服器端）</summary>
        public string? UploadedFilePath { get; set; }

        /// <summary>
        /// 設定檔編號
        /// </summary>
        public string TransferMappingCode { get; set; }
        public List<TableExcelSheetVM> Sheets { get; set; } = new();

        /// <summary>
        /// 供列表頁顯示用：所有已對應的資料表名稱（逗號分隔）
        /// </summary>
        public string? MappingTables =>
            Sheets.Count > 0
                ? string.Join(",", Sheets.Select(s => s.TargetTableName).Where(t => !string.IsNullOrEmpty(t)))
                : null;
    }

    /// <summary>
    /// 每個 Sheet 的對應設定
    /// </summary>
    public class TableExcelSheetVM
    {
        /// <summary>工作表名稱</summary>
        public string SrcSheetName { get; set; } = "";
        /// <summary>在來源檔案中的工作表索引（0-based，CSV 固定為 0）</summary>
        public int SrcSheetIndex { get; set; } = 0;
        /// <summary>標題列起始行（1-based）</summary>
        public int HeaderRowIndex { get; set; } = 1;

        public string DBName { get; set; }

        /// <summary>對應資料表名稱</summary>
        public string TargetTableName { get; set; } = "";

        /// <summary>
        /// 對應資料表名稱的說明
        /// </summary>
        public string TargetTableNameComment { get; set; }
        public List<TableExcelMappingVM> Mappings { get; set; } = new();

        /// <summary>篩選條件（JSON 字串）</summary>
        public string? FilterCondition { get; set; }

        /// <summary>篩選條件模式：'builder' 或 'manual'</summary>
        public string? FilterMode { get; set; }
    }

    /// <summary>
    /// 單筆欄位對應（Excel 欄位 → DB 欄位）
    /// </summary>
    public class TableExcelMappingVM
    {
        /// <summary>檔案 (Excel/CSV) 欄位名稱</summary>
        public string SrcFileColumnName { get; set; } = "";
        /// <summary>資料表對應欄位名稱</summary>
        public string TargetTableColumnName { get; set; } = "";

        /// <summary>是否為主鍵</summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>是否加密</summary>
        public bool IsEncrypt { get; set; }
        /// <summary>當來源欄位值為 Null 時填入的預設值</summary>
        public string? DefaultValue { get; set; }
    }
}
