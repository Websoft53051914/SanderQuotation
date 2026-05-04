namespace MES.Models
{
    public class AlertMsgRedirection
    {
        public List<string> Msgs { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public string AlertType { get; set; }
        public string ClassName { get; set; }

        public Dictionary<string, object> Paras { get; set; }
        public string ParasJson { get; set; }

        public Guid? Id { get; set; }
    }
}
