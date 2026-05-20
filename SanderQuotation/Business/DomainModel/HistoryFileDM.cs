using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public class HistoryFileDM : BaseDM
    {
        public string FileName { get; set; } = "";
        public string UploadId { get; set; } = "";
        public string? FileSummary { get; set; }
    }
}
