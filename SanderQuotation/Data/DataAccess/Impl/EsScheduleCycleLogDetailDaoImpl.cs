using Core.Utility.Base.Data;
using Core.Utility.Extensions;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class EsScheduleCycleLogDetailDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleLogDetailEntity>, IEsScheduleCycleLogDetailDAO
    {

        public List<EsScheduleCycleLogDetailWithLogDTO> GetDetailsByCode(string scheduleCycleCode, DateTime? dateFrom, DateTime? dateTo)
        {
            var param = new Dictionary<string, object>
            {
                { "code", scheduleCycleCode },

            };
            string whereSQL = "";
            if(dateFrom.HasValue)
            {
                param.Add("dateFrom", dateFrom.Value);
                whereSQL += " AND l.RunAt >= @dateFrom ";
            }
            if(dateTo.HasValue)
            {
                param.Add("dateTo", dateTo.Value.AddDays(1).AddSeconds(-1));
                whereSQL += " AND l.RunAt <= @dateTo ";
            }

            string sql = $@"
                SELECT d.*,
                       l.ScheduleCycleCode, l.RunAt AS LogRunAt, l.DurationMs AS LogDurationMs,
                       l.Status AS LogStatus, l.TriggerType
                FROM esScheduleCycleLogDetail d
                INNER JOIN esScheduleCycleLog l ON l.Id = d.ScheduleCycleLogId
                INNER JOIN esScheduleCycle c ON c.ScheduleCycleCode = l.ScheduleCycleCode
                WHERE l.ScheduleCycleCode = @code
                 {whereSQL}
                ORDER BY l.RunAt DESC, d.CreatedAt ASC";

            return DbHelper.FindList<EsScheduleCycleLogDetailWithLogDTO>(sql, param);
        }

        public void DeleteOldLog(int days)
        {
            var targetDate = DateTime.Now.AddDays(-days);
            Dictionary<string, object> param = new() { { "targetDate", targetDate } };

            string sql = @"
DELETE FROM esschedulecyclelogdetail AS d
USING esschedulecyclelog AS l
WHERE d.schedulecyclelogid = l.id
  AND l.createdat < @targetDate";

            DbHelper.Execute(sql, param);
        }
    }
}
