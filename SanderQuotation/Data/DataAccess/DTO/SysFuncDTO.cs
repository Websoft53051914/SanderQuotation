using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class SysFuncDTO : TB_SysFuncEntity
    {
        public string? PermissionCode { get; set; }
        public string? ClassName { get; set; }
        public string? SysFuncName { get; set; }
        public string? DetailName { get; set; }
        public Guid? FilterFuncClassId { get; set; }
        public int? FilterStatus { get; set; }
    }
}
