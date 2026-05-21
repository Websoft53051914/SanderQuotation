using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel.HistoryFile
{
    public class HistoryFileEditVM
    {
        /// <summary>
        /// 新上傳檔案
        /// </summary>
        public List<string> NewFileUploadIdList { get; set; } = [];

        public Guid Id { get; set; }

        public string FileSummary { get; set; } = string.Empty;
    }
}
