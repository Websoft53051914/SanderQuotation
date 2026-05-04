namespace ViewModel
{
    public class SysFuncVM : BaseVM
    {
        public string Name { set; get; }
        public string ClassName { set; get; }

        public Guid? FuncClassId { set; get; }

        public string URL { set; get; }

        public string Sequence { set; get; }


        //public int Status { set; get; }
        public string Memo { set; get; }
        public string StatusName { get; set; }


        public List<SysFuncDetailVM> FuncDetails { get; set; }







        public List<string> Codes { get; set; } = new List<string>()
        {
            { ""} ,
            { ""} ,
            { ""} ,
            { ""} ,
        };

        public List<string> PerCodes { get; set; } = new List<string>();

    }
}
