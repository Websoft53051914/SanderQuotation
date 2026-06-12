using Core.Utility.Base.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("ESDbTransferMappingColumn")]
    public class ESDbTransferMappingColumnEntity : SP_BaseEntity
    {
        public string Type { set; get; }
        public string SortNo { set; get; }
        public string Priority { set; get; }

        public string TransferMappingCode { set; get; }
        public string SrcColumnName { set; get; }
        public string DstColumnName { set; get; }

        // ✅ 修正：DB 為 bool NULL
        public bool? IsEncrypt { set; get; }
        public bool? IsPrimaryKey { set; get; }
    }
}
