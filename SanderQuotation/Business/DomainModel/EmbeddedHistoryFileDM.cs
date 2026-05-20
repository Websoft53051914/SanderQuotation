using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DomainModel
{
    public class EmbeddedHistoryFileDM:BaseDM
    {
        public Guid HistoryFileId { get; set; }
    
            /// <summary>
            /// 向量資料
            /// </summary>
            public float[] Embedding { get; set; } = [];
    }
}
