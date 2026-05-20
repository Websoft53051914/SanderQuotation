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

        //額外欄位
        /// <summary>
        /// 向量資料
        /// </summary>
        public float[] Embedding { get; set; } = [];

        public string UpdatedByName { set; get; } = string.Empty;
    }

}
