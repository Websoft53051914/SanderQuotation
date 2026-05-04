using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClass.Model
{
    public class PageSortInfo
    {   
        /// <summary>
        /// 排序欄位
        /// </summary>
        public string Column { get; set; } = "";
        /// <summary>
        /// 排序方式
        /// </summary>
        public string Direction { get; set; } = "";

    }
}
