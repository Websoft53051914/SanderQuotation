using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class GeneralInsertInputDataDTO<T> where T:class
    {
        /// <summary>
        /// 業務物件 JSON
        /// </summary>
        public T Data { get; set; }
    }
}
