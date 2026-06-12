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

        // ✅ 修正：DB 為 timestamp NULL
        public DateTime? RunAt { get; set; }

        public int? DurationMs { get; set; }
        public string DBTransferCode { get; set; }

        // ✅ 新增：DB 有此欄位但 Entity 缺少
        public string DbTransferCsvCode { get; set; }

        public string FileTransferCode { get; set; }

        // ✅ 新增：DB 有此欄位但 Entity 缺少
        public string TransferCode { get; set; }

        public string JobStatus { get; set; }
        public string ErrorMessage { get; set; }
        public int? OtherActionType { get; set; }
    }
}
