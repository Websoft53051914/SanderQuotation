using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Entity
{
    [Table("EmbeddedHistoryFile")]
    public class EmbeddedHistoryFileEntity : SP_BaseEntity
    {
        public Guid HistoryFileId { get; set; }

        /// <summary>
        /// 向量資料
        /// </summary>
        public float[]? Embedding { get; set; } = null;
    }
}
