namespace ViewModel
{
    public class SysFuncClassVM : BaseVM
    {

        public string ClassName { set; get; }

        public int? Sequence { set; get; }

        public bool Selected { get; set; }
        public string Memo { set; get; }
        public string StatusName { set; get; }
    }
}
