using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    public class SP_BaseEntity : Core.Utility.Base.Data.GuidId.BaseEntity
    {
        static DateTime dtNow = DateTime.Now;
        public int? Status { set; get; }

        public DateTime? CreatedAt { set; get; } = dtNow;

        public DateTime? UpdatedAt { set; get; } = dtNow;

        public string CreatedBy { set; get; }

        public string UpdatedBy { set; get; }
    }
}
