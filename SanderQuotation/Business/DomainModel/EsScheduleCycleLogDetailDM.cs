namespace Business.DomainModel
{
    public class EsScheduleCycleLogDetailDM : BaseDM
    {
        public Guid ScheduleCycleLogId { get; set; }

        public int DataCount { get; set; }

        public int ErrorCount { get; set; }

        public string DBTransferCode { get; set; }

        public string FileTransferCode { get; set; }

        public DateTime RunAt { get; set; }

        public int? DurationMs { get; set; }

        /// <summary>Success / PartialFail / Failed</summary>
        public string JobStatus { get; set; }

        /// <summary>任務層級例外訊息（DB連線失敗等）</summary>
        public string ErrorMessage { get; set; }

        public List<EsTransferErrorLogDM> ErrorLogs = new();

        /// <summary>取得該筆 Detail 的 TransferCode（依優先順序：DB轉入 > 轉出CSV > 檔案轉入）</summary>
        public string GetTransferCode()
        {
            if (!string.IsNullOrEmpty(DBTransferCode)) return DBTransferCode;
            return FileTransferCode;
        }

        /// <summary>取得中文類型名稱：資料轉入 / 轉出檔案 / 檔案轉入</summary>
        public string GetTransferTypeDisplay()
        {
            if (!string.IsNullOrEmpty(DBTransferCode)) return "資料轉入";
            if (!string.IsNullOrEmpty(FileTransferCode)) return "檔案轉入";
            return "未知類型";
        }
    }
}
