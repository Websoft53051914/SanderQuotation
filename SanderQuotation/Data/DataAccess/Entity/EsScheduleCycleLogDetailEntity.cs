using Core.Utility.Base.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("esScheduleCycleLogDetail")]
    public class EsScheduleCycleLogDetailEntity : SP_BaseEntity
    {
        public Guid ScheduleCycleLogId { get; set; }

        public int DataCount { get; set; }

        public int ErrorCount { get; set; }

        public DateTime RunAt { get; set; }

        public int? DurationMs { get; set; }


        public string DBTransferCode { get; set; }

        public string FileTransferCode { get; set; }

        /// <summary>Success / PartialFail / Failed</summary>
        public string JobStatus { get; set; }

        /// <summary>任務層級例外訊息（DB連線失敗等）</summary>
        public string ErrorMessage { get; set; }

        /// <summary>其他排程動作類型（對應 ScheduleCycleActionTypeEnum）</summary>
        public int? OtherActionType { get; set; }
    }
}
