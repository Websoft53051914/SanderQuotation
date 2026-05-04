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

        // FK 指向主表 ESDbTransferMapping
        public string TransferMappingCode { set; get; }

        // 來源欄位名稱
        public string SrcColumnName { set; get; }

        // 目標欄位名稱
        public string DstColumnName { set; get; }
        public bool IsEncrypt { set; get; }
        public bool IsPrimaryKey { set; get; }
    }
}
