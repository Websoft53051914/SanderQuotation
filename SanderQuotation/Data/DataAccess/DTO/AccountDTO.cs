using Core.Utility.Base.Data;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class AccountDTO : TB_AccountEntity
    {
        public string? RoleName { get; set; }
        public Guid RoleId { get; set; }

        /// <summary>
        /// 員工資料代號
        /// </summary>
        public int? EmployeeId { get; set; }
        
        /// <summary>
        /// 角色名稱列表
        /// </summary>
        public List<string> RoleNames { get; set; } = new List<string>();
    }
}

