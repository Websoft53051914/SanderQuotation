namespace Business.DomainModel
{
    public class EsScheduleCycleLogDM : BaseDM
    {
        public string ScheduleCycleCode { get; set; }

        public DateTime RunAt { get; set; }

        public int? DurationMs { get; set; }

        public string Status { get; set; }

        public string Type { get; set; }


        public string TriggerType { get; set; }

        public List<EsScheduleCycleLogDetailDM> Details { get; set; } = new List<EsScheduleCycleLogDetailDM>();
    }
}
