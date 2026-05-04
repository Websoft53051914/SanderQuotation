using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClass.Model
{
    public class PermissionCheckResultDTO
    {
        public string IsSuccess { get; set; } = "";
      
        public string ReturnCode { get; set; } = "";
        public string ReturnMessage { get; set; } = "";
    }
}
