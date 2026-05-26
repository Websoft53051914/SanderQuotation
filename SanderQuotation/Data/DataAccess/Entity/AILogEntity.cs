using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    [Table("AILog")]
    public class AILogEntity:SP_BaseEntity
    {
        public string Account { get; set; } = string.Empty;

        public int Role { get; set; }

        public string? Content { get; set; }

        public string? Sql { get; set; }

        public string? Function { get; set; }

        public DateTime LogTime { get; set; }

    }
}
