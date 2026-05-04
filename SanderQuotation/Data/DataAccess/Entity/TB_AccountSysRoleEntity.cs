using Core.Utility.Base.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    [Table("TB_AccountSysRole")]
    public class TB_AccountSysRoleEntity : SP_BaseEntity
    {
        public Guid RoleId { set; get; }
        public Guid AccountId { set; get; }
    }
}
