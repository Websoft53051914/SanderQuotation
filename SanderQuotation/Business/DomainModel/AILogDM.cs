using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public class AILogDM:BaseDM
    {
        public string Account { get; set; } = string.Empty;

        public int Role { get; set; }

        public string? Content { get; set; }

        public string? Sql { get; set; }

        public string? Function { get; set; }
    }
}
