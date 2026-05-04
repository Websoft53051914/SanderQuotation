using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public class MenuDM
    {
        public MenuDM()
        {
        }
        public string ClassName { set; get; }

        public int PermissionCode { set; get; }
        public string FuncName { set; get; }
        public Guid FuncId { set; get; }
        public string Url { set; get; }

        public int? DataCount { set; get; }
    }
}
