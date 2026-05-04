using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClass.Model
{
    public class UserInfoFromTokenResultDTO
    {
        public string UserAccount { get; set; } = "";
        public List<string> RoleList { get; set; } = [];
        public List<string> DeptList { get; set; } = [];
        public string ClientIP { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string TokenValue { get; set; } = "";
        public string CompanyID { get; set; } = "";
    }
}
