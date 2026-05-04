using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class SysFuncDetailDTO : TB_SysFuncDetailEntity
    {
        public string FuncName { set; get; }

    }
}
