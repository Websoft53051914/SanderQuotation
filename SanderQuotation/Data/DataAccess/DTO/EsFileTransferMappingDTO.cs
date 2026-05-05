using Data.DataAccess.Entity;

namespace Data.DataAccess.DTO
{
    public class EsFileTransferMappingDTO : EsFileTransferMappingEntity
    {
        public string? Keyword1 { get; set; }

        /// <summary>對應資料表（逗號分隔，由 EsFileTransferMappingColumn JOIN 取得）</summary>
        public string? MappingTables { get; set; }
    }
}
