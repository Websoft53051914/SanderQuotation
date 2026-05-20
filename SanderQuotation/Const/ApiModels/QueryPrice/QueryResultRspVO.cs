using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.QueryPrice
{   
    public class QueryActionResultRspVO
    {
        public class QueryResultRspVO
        {
            /// <summary>
            /// 外部查價日期
            /// </summary>
            public DateTime? QuotationDate { get; set; }

            /// <summary>
            /// 外部單價(原幣)
            /// </summary>
            public decimal? UnitPriceOriginalCurrency { get; set; }

            /// <summary>
            /// 外部單價(台幣)
            /// </summary>
            public decimal? UnitPriceTwd { get; set; }

            /// <summary>
            /// 外部最小訂購量(MOQ)
            /// </summary>
            public int? Moq { get; set; }

            /// <summary>
            /// 外部幣別
            /// </summary>
            public string? ExternalCurrency { get; set; }

            /// <summary>
            /// 外部供應商名稱
            /// </summary>
            public string SupplierName { get; set; }
        }

        public QueryResultRspVO? Result { get; set; }
        public List<DecisionLogVO> DecisionLogs { get; set; } = new();
        public class DecisionLogVO
        {
            /// <summary>
            /// 階段
            /// </summary>
            public int? Stage { get; set; }

            /// <summary>
            /// 步驟
            /// </summary>
            public int? Step { get; set; }

            /// <summary>
            /// 紀錄訊息
            /// </summary>
            public string? Message { get; set; }
        }
       
    }
    
}
