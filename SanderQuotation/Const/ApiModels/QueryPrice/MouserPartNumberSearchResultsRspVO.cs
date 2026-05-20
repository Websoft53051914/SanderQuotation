using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.QueryPrice
{
    public class MouserPartNumberSearchResultsRspVO
    {
        public List<MouserRspErrorVO> Errors { get; set; } = new List<MouserRspErrorVO>();
        public SearchResult SearchResults { get; set; } = new SearchResult();

        public class SearchResult
        {
            /// <summary>
            /// 搜尋結果總數
            /// </summary>
            public int NumberOfResult { get; set; }

            /// <summary>
            /// 搜尋結果列表
            /// </summary>
            public List<SearchPart> Parts { get; set; } = new List<SearchPart>();
        }

        public class SearchPart
        {
            /// <summary>
            /// 廠商型號
            /// </summary>
            public string ManufacturerPartNumber { get; set; } = "";

            /// <summary>
            /// Mouser料號
            /// </summary>
            public string MouserPartNumber { get; set; } = "";

            /// <summary>
            /// 製造商名稱
            /// </summary>
            public string Manufacturer { get; set; } = "";

            /// <summary>
            /// 可用庫存數量
            /// </summary>
            public int AvailabilityInStock { get; set; }

            /// <summary>
            /// 標準階梯定價陣列
            /// </summary>
            public List<SearchResultPriceBreak> PriceBreaks { get; set; } = new List<SearchResultPriceBreak>();
        }

        public class SearchResultPriceBreak
        {
            /// <summary>
            /// 起始數量
            /// </summary>
            public int Quantity { get; set; }

            /// <summary>
            /// 單價 (修正：Mouser API 回傳為 string，例如 "$1.25" 或 "1.25")
            /// </summary>
            public string Price { get; set; } = "";

            /// <summary>
            /// 幣別
            /// </summary>
            public string Currency { get; set; } = "";
        }
    }
}
