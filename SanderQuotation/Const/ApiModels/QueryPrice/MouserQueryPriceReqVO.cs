using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.QueryPrice
{
    public class MouserQueryPriceReqVO
    {
        public SearchByPart SearchByPartRequest { get; set; } = new();
        public class SearchByPart
        {
            /// <summary>
            /// 要查詢的料號
            /// </summary>
            public string mouserPartNumber { get; set; }

            public string partSearchOptions { get; } = "Exact";
        }
    }
}
