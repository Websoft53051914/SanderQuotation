namespace CommonClass.Models
{
    public class DispatcherReturnMsg
    {
        public string IsSuccess { get; set; }
        public string AlertLevel { get; set; }
        public string ReturnCode { get; set; }
        public string ReturnMsg { get; set; }
        public int AffectedCount { get; set; }
        //public string? EntityID { get; set; }
    }
}
