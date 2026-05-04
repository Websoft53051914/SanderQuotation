using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public partial class SysRoleDM : BaseDM
    {
        public string? RoleName { get; set; }
        public int? Status { get; set; }
        public string? Memo { get; set; }
        /// <summary>
        /// 角色 (1.系統管理員 2.客服人員 3. 技師)(enum)
        /// </summary>
    }

    public partial class SysRoleDM 
    {
        public List<string> PerCodes { get; set; }
        public List<string> FuncDetailIds { get; set; }
    }
}