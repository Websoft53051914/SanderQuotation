using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.LEDCCDevice
{
    public class SetMultiLineDisplayReqVO
    {
        /// <summary>
        /// IP
        /// </summary>
        public string IP { get; set; } = string.Empty;

        public string Line1 { get; set; } = string.Empty;

        public string Line2 { get; set; } = string.Empty;
    }
}
