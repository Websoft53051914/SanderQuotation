using Core.Utility.Base.Data.GuidId;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    [Table("HistoryFile")]
    public class HistoryFileEntity: SP_BaseEntity
    {
        public string FileName { get; set; } = "";
        public string UploadId { get; set; } = "";
        public string? FileSummary { get; set; }
    }
}
