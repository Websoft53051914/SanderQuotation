
namespace ViewModel
{
    public partial class MemberVM : BaseVM
    {
        public string? MEMBERACCOUNT { get; set; }
        public string? ACCOUNTNAME { get; set; }
        public int? PERMISSIONID { get; set; }
        public string? ACCOUNTSTATUS { get; set; }
        public DateTime LASTLOGINTIME { get; set; }
        public string? MEMBERPWD { get; set; }
        public DateTime? LASTMEMBERPWDTIME { get; set; }
        public string? ACCOUNTEMAIL { get; set; }
        public string? RESETPWDCODE { get; set; }
        public DateTime LASTFORGETPWDTIME { get; set; }
        public int LOGINS { get; set; }
        public DateTime? LOCKTIME { get; set; }
        public DateTime? LOGOUTTIME { get; set; }
        public string? TESTA { get; set; }
        public string? TESTAC { get; set; }

        //public EditEngineerVM? engineerData { get; set; }

        public string? EnginnerNo { get; set; }

        public string? Name { get; set; }

        public string? Expertise { get; set; }

        public string? Gender { get; set; }

        public List<string> ServiceCityIds { get; set; } = new List<string>();


        public string? ContactCityId { get; set; }

        public string? ContactTownId { get; set; }

        public string? Address { get; set; }


        public string? Phone { get; set; }

        public string? Mobile { get; set; }

        public string? LicenseName { get; set; }

        //public byte[] LicenseFile { get; set; }

        //public string? DaysAvailable { get; set; }

        //public string? TimesAvailable { get; set; }

        public string? Level { get; set; }

        public string? LineName { get; set; }
        /// <summary>
        /// 技師類型(1:綠瓦技師,2:委外技師)
        /// </summary>
        public string EngineerType { get; set; } = "";

        public string? engineerData { get; set; }



    }

    public partial class MemberVM
    {
        public List<string> PermissionIDs { get; set; }
        public bool IsLock { get; set; }
        public string? RoleName { get; set; }
        public long RoleId { get; set; }



    }
}
