using Core.Utility.Base.Data;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{
    public class EsTransferErrorLogDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsTransferErrorLogEntity>, IEsTransferErrorLogDAO
    {
        public List<EsTransferErrorLogEntity> FindListByScheduleCode(string scheduleCycleCode)
        {
            string sql = $@"SELECT el.* FROM esTransferErrorLog el
            INNER JOIN esScheduleCycleLogDetail d ON d.Id = el.ScheduleCycleLogDetailId
            INNER JOIN esScheduleCycleLog l ON l.Id = d.ScheduleCycleLogId
            WHERE l.ScheduleCycleCode = @code";

            var param = new Dictionary<string, object>
            {
                { "code", scheduleCycleCode }
            };

            return DbHelper.FindList<EsTransferErrorLogEntity>(sql, param);
        }
    }
}
