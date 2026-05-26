using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Impl
{
    public class AILogDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<AILogEntity>, IAILogDAO
    {
        public void DeleteOldLog(int days)
        {
            string sql = $"DELETE FROM AILog WHERE LogTime < @CutoffDate";
            DateTime cutoffDate = DateTime.Now.AddDays(-days);
            DbHelper.Execute(sql, new Dictionary<string, object>()
            {
                {"CutoffDate", cutoffDate }
            });
            DbHelper.Commit();
        }
    }
}
