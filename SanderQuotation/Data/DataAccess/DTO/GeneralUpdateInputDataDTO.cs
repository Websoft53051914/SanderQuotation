using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class GeneralUpdateInputDataDTO<T> where T : class
    {
        public long? KeyValue { get; set; }   //RowGuid

        /// <summary>
        /// 業務物件 JSON
        /// </summary>
        public T Data { get; set; }
    }
}
