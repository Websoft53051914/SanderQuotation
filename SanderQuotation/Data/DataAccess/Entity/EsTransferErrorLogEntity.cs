using Core.Utility.Base.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("esTransferErrorLog")]
    public class EsTransferErrorLogEntity : Core.Utility.Base.Data.GuidId.BaseEntity
    {
        public string Exception { get; set; }

        public string Sql { get; set; }

        public Guid ScheduleCycleLogDetailId { get; set; }
    }
}
