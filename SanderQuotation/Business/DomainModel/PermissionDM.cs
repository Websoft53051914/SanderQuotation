namespace Business.DomainModel
{
    public class PermissionDM
    {
        public long FunctionId { get; set; }
        public string? FunctionName { get; set; }



        public string? FuncDetailName { get; set; }
        public Guid FuncDetailId { get; set; }
        public string? ClassName { get; set; }
        public string? FuncName { get; set; }
        public string? Sequence { get; set; }
        public string? IsPermission { get; set; }
    }
}
