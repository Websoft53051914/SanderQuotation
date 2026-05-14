using Const;
using Core.Utility.Extensions;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class EsScheduleCycleOtherTransferDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleOtherTransferEntity>, IEsScheduleCycleOtherTransferDAO
    {
        public List<EsScheduleCycleOtherTransferEntity> GetListbyFilter(SearchVO searchVO)
        {
            Dictionary<string, object> param = new Dictionary<string, object>();
            string whereSQL = $" AND {nameof(EsScheduleCycleOtherTransferEntity.Status)} = @Status";
            param.Add("Status", StatusEnum.Enabled.ToInt());
            if (searchVO.ScheduleCycleCodeIn.Count > 0)
            {
                whereSQL += $" AND {nameof(EsScheduleCycleOtherTransferEntity.ScheduleCycleCode)} = ANY(@ScheduleCycleCodeIn)";
                param.Add("ScheduleCycleCodeIn", searchVO.ScheduleCycleCodeIn);
            }
            string sql = @"SELECT * FROM EsScheduleCycleOtherTransfer WHERE 1=1" + whereSQL;
            return DbHelper.FindList<EsScheduleCycleOtherTransferEntity>(sql, param);
        }
    }
}
