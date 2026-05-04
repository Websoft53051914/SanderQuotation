using Core.Utility.Base.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    [Table("TB_SysRole")]
    public class TB_SysRoleEntity : SP_BaseEntity
    {
        public string? RoleName { get; set; }
        public string? Memo { get; set; } = null;
    }
}
