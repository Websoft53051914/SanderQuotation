using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.LicenseRecognitionBlockAllowApiModels
{
    public class AuditBlockAllowListReqVO
    {
        public List<CarVO> LicensePlateInfoList { get; set; } = new List<CarVO>();
        public class CarVO
        {
            public string LicensePlate { get; set; } = "";

            public string listType { get; set; } = "";

            public DateTime createTime { get; set; }

            public DateTime effectiveStartDate { get; set; }

            public DateTime effectiveTime { get; set; }

            public string id { get; set; } = "";
        }
    }
}
