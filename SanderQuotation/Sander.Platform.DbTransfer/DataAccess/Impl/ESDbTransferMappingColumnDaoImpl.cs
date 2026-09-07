using Const;
using Core.Utility.Base.Data;
using Core.Utility.Extensions;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class ESDbTransferMappingColumnDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<ESDbTransferMappingColumnEntity>, IESDbTransferMappingColumnDAO
    {
        public List<ESDbTransferMappingColumnEntity> FindListByFilter(SearchVO searchVO)
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
            string sql = @"SELECT * FROM ESDbTransferMappingColumn WHERE 1=1 "+whereSql;
            return base.DbHelper.FindList<ESDbTransferMappingColumnEntity>(sql, param);
        }
    }
}
