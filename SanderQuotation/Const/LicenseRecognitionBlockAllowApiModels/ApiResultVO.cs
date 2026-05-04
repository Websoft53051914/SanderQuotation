using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.LicenseRecognitionBlockAllowApiModels
{
    public class ApiResultVO
    {
        public int statusCode { get; set; }

        public string statusString { get; set; } = "";

        public string subStatusCode { get; set; } = "";
    }
}
