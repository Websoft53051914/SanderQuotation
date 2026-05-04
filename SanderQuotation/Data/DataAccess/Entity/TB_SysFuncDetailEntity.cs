using Core.Utility.Base.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{


    [Table("TB_SysFuncDetail")]
    public class TB_SysFuncDetailEntity : SP_BaseEntity
    {

        public string Name { set; get; }

        public Guid FuncId { set; get; }

        public string Sequence { set; get; }

        public string PermissionCode { set; get; }




    }
}
