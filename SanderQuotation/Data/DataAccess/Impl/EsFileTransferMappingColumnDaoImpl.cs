using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{
    public class EsFileTransferMappingColumnDaoImpl : BaseImpl<EsFileTransferMappingColumnEntity>, IEsFileTransferMappingColumnDAO
    {
        public List<EsFileTransferMappingColumnDTO> GetListByMappingSettingId(string transferMappingCode)
        {
            var paras = new Dictionary<string, object>
            {
                { "@transferMappingCode", transferMappingCode }
            };

            string sql = $@"SELECT c.*
FROM EsFileTransferMappingColumn c
WHERE c.transferMappingCode = @transferMappingCode
ORDER BY c.{nameof(EsFileTransferMappingColumnEntity.SrcSheetIndex)}, c.{nameof(EsFileTransferMappingColumnEntity.SortNo)}";

            return DbHelper.FindList<EsFileTransferMappingColumnDTO>(sql, paras);
        }
    }
}
