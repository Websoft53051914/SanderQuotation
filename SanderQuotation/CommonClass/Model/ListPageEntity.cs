using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClass.Model
{
    public class ListPageEntity
    {

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }

        public string SortField { get; set; } = "No";
        public string SortDir { get; set; } = "asc";
        /// <summary>
        /// 資料總數
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// 資料總頁數
        /// </summary>
        public int TotalPage { get; set; }

    }

}
