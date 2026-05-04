namespace ViewModel
{
    public class SysFuncDetailVM : BaseVM
    {
        public string Name { set; get; }
        public Guid FuncId { set; get; }
        public string Sequence { set; get; }
        public string PermissionCode { set; get; }
        //public int Status { get; set; }

        public string FuncName { set; get; }


        public List<string> PerCodes { get; set; }
        public List<string> Codes { get; set; }

        public string HideFuncName { set; get; }
    }
}
