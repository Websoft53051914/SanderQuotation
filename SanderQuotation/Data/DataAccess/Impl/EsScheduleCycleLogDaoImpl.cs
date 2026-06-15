using Core.Utility.Base.Data;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{
    public class EsScheduleCycleLogDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleLogEntity>, IEsScheduleCycleLogDAO
    {
        public void DeleteOldLog(int days)
        {
            var targetDate = DateTime.Now.AddDays(-days);
            Dictionary<string, object> param = new() { { "targetDate", targetDate } };  

            string sql = @"
DELETE FROM esschedulecyclelog
WHERE createdat < @targetDate";

            DbHelper.Execute(sql, param);
        }
    }
}
