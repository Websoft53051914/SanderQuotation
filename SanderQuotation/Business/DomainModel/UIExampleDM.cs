namespace Business.DomainModel
{
    public partial class UIExampleDM : BaseDM
    {
        public string TempText { get; set; }
        public string IdCard { get; set; }
        public string CellPhone { get; set; }
        public string HomePhone { get; set; }
        public string Start_Date { get; set; }
        public string End_Date { get; set; }
        public string TempTextArea { get; set; }
        public string TempEMAIL { get; set; }
        public string TempDropdownList { get; set; }
        public string TempCheckBox { get; set; }
        public string TempRadio { get; set; }
        public string Status { get; set; }
    }


    public partial class UIExampleDM : BaseDM
    {
        public Guid FileId { get; set; }
    }

}