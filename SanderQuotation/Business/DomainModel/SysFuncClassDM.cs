using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public class SysFuncClassDM : BaseDM
    {
        public string ClassName { set; get; }

        public int? Sequence { set; get; }

        public int? Status { set; get; }
        public string Memo { set; get; }

    }
}
