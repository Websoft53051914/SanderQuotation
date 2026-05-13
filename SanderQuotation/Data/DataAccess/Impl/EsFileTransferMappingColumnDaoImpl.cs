using Const;
using Core.Utility.Base.Data.GuidId;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class EsFileTransferMappingColumnDaoImpl : BaseImpl<EsFileTransferMappingColumnEntity>, IEsFileTransferMappingColumnDAO
    {
        public List<EsFileTransferMappingColumnDTO> GetListByMappingSettingId(string transferMappingCode)
        {
            var paras = new Dictionary<string, object>
            {
                { "@transferMappingCode", transferMappingCode },
                { "@Status", StatusEnum.Enabled.ToInt() }
            };

            string sql = $@"SELECT c.*
FROM EsFileTransferMappingColumn c
WHERE c.transferMappingCode = @transferMappingCode AND c.Status = @Status
ORDER BY c.{nameof(EsFileTransferMappingColumnEntity.SrcSheetIndex)}, c.{nameof(EsFileTransferMappingColumnEntity.SortNo)}";

            return DbHelper.FindList<EsFileTransferMappingColumnDTO>(sql, paras);
        }

        public List<EsFileTransferMappingColumnEntity> FindListByFilter(SearchVO searchVO)
        {
            Dictionary<string, object> param = new Dictionary<string, object>();
            string whereSql = "";
            whereSql += " AND Status = @Status ";
            param.Add("Status", StatusEnum.Enabled.ToInt());
            if (searchVO.TransferMappingCodeIn != null && searchVO.TransferMappingCodeIn.Count > 0)
            {
                param.Add("TransferMappingCodeIn", searchVO.TransferMappingCodeIn);
                whereSql += " AND TransferMappingCode = ANY(@TransferMappingCodeIn) ";
            }
            if (searchVO.TransferCodeIn != null && searchVO.TransferCodeIn.Count > 0)
            {
                param.Add("TransferCodeIn", searchVO.TransferCodeIn);
                whereSql += " AND DBTransferMappingCode = ANY(@TransferCodeIn) ";
            }
            string sql = @"SELECT * FROM ESFileTransferMappingColumn WHERE 1=1 " + whereSql;
            return base.DbHelper.FindList<EsFileTransferMappingColumnEntity>(sql, param);
        }
    }
}
