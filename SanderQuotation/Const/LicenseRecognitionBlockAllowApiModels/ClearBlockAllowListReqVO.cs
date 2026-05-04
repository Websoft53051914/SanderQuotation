using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.LicenseRecognitionBlockAllowApiModels
{
    public class ClearBlockAllowListReqVO
    {
        public List<string> id { get; set; } = new();

        public bool deleteAllEnabled { get; set; } = true;
    }
}
