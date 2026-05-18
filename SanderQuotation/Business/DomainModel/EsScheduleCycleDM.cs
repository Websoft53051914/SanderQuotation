namespace Business.DomainModel
{
    public class EsScheduleCycleDM
    {
        public Guid Id { get; set; }
        public string ScheduleCycleCode { get; set; }

        public string Status { get; set; }
        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public string CycleName { get; set; }
        public bool IsEnabled { get; set; }
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

        // 子表集合（展開用）
        public List<string> WeekDays { get; set; } = new();          // 0~6
        public List<string> MonthDays { get; set; } = new();         // 1~31
        public List<string> DBTransferSettings { get; set; } = new(); // EXP_*
        public List<string> DbCsvTransferSettings { get; set; } = new(); // CSV_*
        public List<string> FileTransferSettings { get; set; } = new(); // IMP_*
        public List<int> OtherTransferSettings { get; set; } = new(); // 其他類型的設定（如：查價)，參照ScheduleCycleActionTypeEnum來定義具體的值

        public int No { get; set; }
    }
}
