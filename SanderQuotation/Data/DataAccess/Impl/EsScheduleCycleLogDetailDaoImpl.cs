using Core.Utility.Base.Data;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{
    public class EsScheduleCycleLogDetailDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleLogDetailEntity>, IEsScheduleCycleLogDetailDAO
    {
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
