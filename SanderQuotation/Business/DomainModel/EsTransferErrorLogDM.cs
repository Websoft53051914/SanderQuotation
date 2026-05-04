namespace Business.DomainModel
{
    public class EsTransferErrorLogDM : BaseDM
    {
        public string Exception { get; set; }

        public string Sql { get; set; }

        public Guid ScheduleCycleLogDetailId { get; set; }
    }
}
