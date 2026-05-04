using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClass.Model
{
    public class CommonSearchQuery : ListPageEntity
    {
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string Status { get; set; }
    }
}
