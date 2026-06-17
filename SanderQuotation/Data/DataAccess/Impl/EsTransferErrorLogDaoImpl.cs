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

        public void DeleteOldLog(int days)
        {
            var targetDate = DateTime.Now.AddDays(-days);
            Dictionary<string, object> param = new() { { "targetDate", targetDate } };

            string sql = @"
DELETE FROM estransfererrorlog AS el
USING esschedulecyclelogdetail AS d, esschedulecyclelog AS l
WHERE el.schedulecyclelogdetailid = d.id
  AND d.schedulecyclelogid = l.id
  AND l.createdat < @targetDate";

            DbHelper.Execute(sql, param);
        }
    }
}
