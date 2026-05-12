namespace ViewModel
{
    public partial class SysRoleVM : BaseVM
    { 

        public int No { set; get; }
        /// <summary>
        /// 角色名稱
        /// </summary>
        public string RoleName { set; get; }



        /// <summary>
        /// 角色說明
        /// </summary>
        public string Memo { set; get; }

        /// <summary>
        /// 頁面名稱
        /// </summary>
        public string PageTitle { set; get; } 


        //public string Status { get; set; }

        public string StatusName { get; set; }
        public List<string> PerCodes { get; set; }
        public string CompanyName { get; set; }
        //public int? DeptId { get; set; }

        //public string Dept_Name { get; set; }
        public string IsDefault { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }


    public partial class SysRoleVM
    {
        public string Organization_Name { get; set; }
        public bool Selected { get; set; }
        public string FuncClassName { get; set; }
        public List<string> FuncDetailIds { get; set; }
    }
}
