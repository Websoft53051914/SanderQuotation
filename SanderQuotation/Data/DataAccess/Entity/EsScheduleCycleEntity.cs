using System.ComponentModel.DataAnnotations;
using Core.Utility.Base.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("esScheduleCycle")]
    public class EsScheduleCycleEntity : SP_BaseEntity
    {
        // ScheduleCycleCode 作為業務主鍵（DB PK）；BaseEntity 的 RowGuid 不使用
        public string ScheduleCycleCode { get; set; }

        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }

        public string CycleName { get; set; }
        public string Description { get; set; }
        public string CycleType { get; set; }
        public string CronExpression { get; set; }

        public int? SecondInterval { get; set; }

        public int? MinuteInterval { get; set; }
        public int? MinuteAtSecond { get; set; }

        public int? HourInterval { get; set; }
        public int? HourAtMinute { get; set; }
        public int? HourAtSecond { get; set; }

        public int? DayInterval { get; set; }
        public string DayAtTime { get; set; }

        public string WeekAtTime { get; set; }

        public string MonthAtTime { get; set; }

        public DateTime? LastRunAt { get; set; }
        public string LastRunStatus { get; set; }
        public string LastRunMessage { get; set; }
    }

    [Table("esScheduleCycleWeekDay")]
    public class EsScheduleCycleWeekDayEntity : SP_BaseEntity
    {
        public string ScheduleCycleCode { get; set; }
        public byte WeekDay { get; set; }   // 0=日 ~ 6=六

        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
    }

    [Table("esScheduleCycleMonthDay")]
    public class EsScheduleCycleMonthDayEntity : SP_BaseEntity
    {
        public string ScheduleCycleCode { get; set; }
        public byte MonthDay { get; set; }  // 1 ~ 31

        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
    }

    [Table("esScheduleCycleDbTransfer")]
    public class EsScheduleCycleDbTransferEntity : SP_BaseEntity
    {
        public string ScheduleCycleCode { get; set; }
        public string TransferCode { get; set; }  // EXP_ORDER / EXP_STOCK ...

        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
    }

    [Table("esScheduleCycleDbCsvTransfer")]
    public class EsScheduleCycleDbCsvTransferEntity : SP_BaseEntity
    {
        public string ScheduleCycleCode { get; set; }
        public string TransferCode { get; set; }

        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
    }

    [Table("esScheduleCycleFileTransfer")]
    public class EsScheduleCycleFileTransferEntity : SP_BaseEntity
    {
        public string ScheduleCycleCode { get; set; }
        public string TransferCode { get; set; }  // IMP_ORDER / IMP_STOCK ...

        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
    }

    [Table("esScheduleCycleLog")]
    public class EsScheduleCycleLogEntity : SP_BaseEntity
    {
        public string ScheduleCycleCode { get; set; }
        public DateTime RunAt { get; set; }
        public int? DurationMs { get; set; }

        public string TriggerType { get; set; }  // Service / Manual
        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
    }
}
