using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels
{
    public class ApiResultVO
    {   
        /// <summary>
        /// 回傳結果
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string Message { get; set; }
    }
}
