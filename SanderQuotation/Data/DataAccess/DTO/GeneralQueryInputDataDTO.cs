using CommonClass.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class GeneralQueryInputDataDTO
    {
        public ListPageEntity Pagination { get; set; } = new();
        /// <summary>
        /// 搜尋條件(查詢分頁用)
        /// </summary>
        public Dictionary<string, object>? Filter { get; set; } = null;
        /// <summary>
        /// 主要用在PK查詢
        /// </summary>
        public Dictionary<string, object>? KeyValue { get; set; } = null;
        /// <summary>
        /// 排序(查詢分頁用)
        /// </summary>
        public PageSortInfo? Sort { get; set; } = null;
    }
}
