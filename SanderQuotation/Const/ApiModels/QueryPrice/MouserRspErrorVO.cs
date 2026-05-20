using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.QueryPrice
{
    public class MouserRspErrorVO
    {
        public long Id { get; set; }
        public string Message { get; set; } = "";
        public string Code { get; set; } = "";
        public string ResourceKey { get; set; } = "";
        public string ResourceFormatString { get; set; } = "";
        public string ResourceFormatString2 { get; set; } = "";
        public string PropertyName { get; set; } = "";
    }
}
