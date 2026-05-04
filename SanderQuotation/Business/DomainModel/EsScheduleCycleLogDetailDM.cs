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
    }
}
