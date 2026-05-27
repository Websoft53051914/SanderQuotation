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
        public List<EsScheduleCycleLogDetailWithLogDTO> GetDetailsByCode(string scheduleCycleCode)
        {
            var param = new Dictionary<string, object>
            {
                { "code", scheduleCycleCode },
                { "Status", StatusEnum.Enabled.ToInt()}
            };

            string sql = @"
                SELECT d.*,
                       l.ScheduleCycleCode, l.RunAt AS LogRunAt, l.DurationMs AS LogDurationMs,
                       l.Status AS LogStatus, l.TriggerType
                FROM esScheduleCycleLogDetail d
                INNER JOIN esScheduleCycleLog l ON l.Id = d.ScheduleCycleLogId
                INNER JOIN esScheduleCycle c ON c.ScheduleCycleCode = l.ScheduleCycleCode
                WHERE l.ScheduleCycleCode = @code AND c.Status = @Status
                ORDER BY l.RunAt DESC, d.CreatedAt ASC";

            return DbHelper.FindList<EsScheduleCycleLogDetailWithLogDTO>(sql, param);
        }
        public void DeleteOldLog(int days)
        {
            var targetDate = DateTime.Now.AddDays(-days);
            Dictionary<string, object> param = new() { { "targetDate", targetDate } };

            string sql = @"
DELETE FROM esScheduleCycleLogDetail
WHERE CreatedAt < @targetDate";

            DbHelper.Execute(sql, param);
            DbHelper.Commit();
        }
    }
}
