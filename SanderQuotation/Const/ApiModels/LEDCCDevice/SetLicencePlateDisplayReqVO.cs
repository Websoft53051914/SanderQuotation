using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.LEDCCDevice
{
    public class SetLicencePlateDisplayReqVO
    {
        /// <summary>
        /// IP
        /// </summary>
        public string IP { get; set; } = string.Empty;
        /// <summary>
        /// 車牌號碼
        /// </summary>
        public string PlateNo { get; set; } = string.Empty;
        /// <summary>
        /// 是否要有滑動
        /// </summary>
        public bool IsAnimation { get; set; }
        /// <summary>
        /// 下方顯示字
        /// </summary>
        public string LowerText { get; set; } = string.Empty;
    }
}
