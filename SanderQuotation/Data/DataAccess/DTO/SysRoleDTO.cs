using Core.Utility.Base.Data;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class SysRoleDTO : TB_SysRoleEntity
    {
        public string? FuncClassName { get; set; }
        
    }
}
