using Data.DataAccess.Entity;

namespace Data.DataAccess.DTO
{
    /// <summary>
    /// EsScheduleCycleLogDetail JOIN EsScheduleCycleLog 的查詢結果
    /// </summary>
    public class EsScheduleCycleLogDetailWithLogDTO : EsScheduleCycleLogDetailEntity
    {
        public string ScheduleCycleCode { get; set; }
        public DateTime LogRunAt { get; set; }
        public int? LogDurationMs { get; set; }
        public string LogStatus { get; set; }
        public string TriggerType { get; set; }
    }
}
