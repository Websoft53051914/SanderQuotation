using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClass.Model
{
    public class UserInfo
    {
        public string AccessToken { get; set; }
        public string UserAccount { get; set; }
        public string CompanyID { get; set; }
        public string UserName { get; set; }
        public string ExpireAt { get; set; }
        public List<string> RoleList { get; set; }
        public string IP { get; set; }

        public string UserAgent { get; set; }

        public string SystemCode { get; set; } = "MUEIP";

        public string ModuleCode { get; set; } = "SYS";
    }
}
