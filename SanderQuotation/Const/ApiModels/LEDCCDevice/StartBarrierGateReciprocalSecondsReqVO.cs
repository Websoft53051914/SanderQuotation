using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.LEDCCDevice
{
    public class StartBarrierGateReciprocalSecondsReqVO
    {
        /// <summary>
        /// IP
        /// </summary>
        public string IP { get; set; } = string.Empty;

        public DateTime TargetDateTime { get; set; }
    }
}
