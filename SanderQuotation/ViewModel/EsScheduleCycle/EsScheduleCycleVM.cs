namespace ViewModel
{
    public class EsScheduleCycleVM
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
        public string WeekDays { get; set; }

        public string MonthAtTime { get; set; }
        public string MonthDays { get; set; }

        public List<string> DBTransferSettings { get; set; } = new();
        public List<string> FileTransferSettings { get; set; } = new();

        public DateTime? LastRunAt { get; set; }
        public string LastRunStatus { get; set; }
        public string LastRunMessage { get; set; }

        public int No { get; set; }
    }

    public class EsScheduleCycleGridVM
    {
        public string ScheduleCycleCode { get; set; }
        public string CycleName { get; set; }
        public string CycleType { get; set; }
        public string CronExpression { get; set; }
        public bool IsEnabled { get; set; }
        public List<string> DBTransferSettings { get; set; } = new();
        public List<string> FileTransferSettings { get; set; } = new();
        public int No { get; set; }
    }
}
