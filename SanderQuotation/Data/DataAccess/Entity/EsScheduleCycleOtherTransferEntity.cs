using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    [Table("esScheduleCycleOtherTransfer")]
    public class EsScheduleCycleOtherTransferEntity : SP_BaseEntity
    {
        public string ScheduleCycleCode { get; set; }
        public int ActionType { get; set; }

        public string Type { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }
    }
}
