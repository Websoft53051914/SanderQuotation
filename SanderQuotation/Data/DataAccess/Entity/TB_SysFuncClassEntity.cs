using Core.Utility.Base.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    [Table("TB_SysFuncClass")]
    public class TB_SysFuncClassEntity : SP_BaseEntity
    {

        public string ClassName { set; get; }

        public int? Sequence { set; get; }

        public string Memo { set; get; }


    }
}
