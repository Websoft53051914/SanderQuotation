using Core.Utility.Base.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{

    [Table("TB_SysFunc")]
    public class TB_SysFuncEntity : SP_BaseEntity
    {

        public string Name { set; get; }

        public Guid FuncClassId { set; get; }

        public string Url { set; get; }

        public string Sequence { set; get; }


        public string Memo { set; get; }
    }
}
