using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Dao
{
    public interface IAILogDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<AILogEntity>
    {
        void DeleteOldLog(int days);
    }
}
