using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public class SysFuncDM : BaseDM
    {
        public string Name { set; get; }
        public string ClassName { set; get; }

        public Guid? FuncClassId { set; get; }

        public string URL { set; get; }

        public string Sequence { set; get; }


        public string Memo { set; get; }

        public string PermissionCode { get; set; }
        //public string ClassName { get; set; }
        public string SysFuncName { get; set; }
        public string DetailName { get; set; }



    }
}
