using Core.Utility.Base.Data;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsScheduleCycleLogDetailDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleLogDetailEntity>
    {
        void DeleteOldLog(int days);
    }
}
