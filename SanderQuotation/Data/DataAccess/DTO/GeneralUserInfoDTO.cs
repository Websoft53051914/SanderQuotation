using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class GeneralUserInfoDTO
    {
        public string TokenValue { get; set; } = "";
        public string ClientIP { get; set; } = "127.0.0.1";
        public string UserAgent { get; set; }
    }
}
