using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public class SysFuncDetailDM:BaseDM
    {
        public string Name { set; get; }
        public Guid FuncId { set; get; }
        public string Sequence { set; get; }
        public string PermissionCode { set; get; }
        public int Status { get; set; }

        public string FuncName { set; get; }

        public List<string> PerCodes { get; set; }
        public List<string> Codes { get; set; }
    }
}
