using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel.HistoryFile
{
    public class HistoryFileGridVM
    {
        public Guid Id { set; get; }

        public int No { get; set; }

        public string FileName { set; get; } = string.Empty;

        public string FileSummary { set; get; } = string.Empty;

        public DateTime UpdatedAt { set; get; }
        public string UpdatedAtStr
        {
            get
            {
                return UpdatedAt.ToString("yyyy/MM/dd HH:mm:ss");
            }
        }

        public string UpdatedByName { set; get; } = string.Empty;
    }
}
