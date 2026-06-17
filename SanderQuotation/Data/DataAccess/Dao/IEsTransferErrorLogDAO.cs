using Core.Utility.Base.Data;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsTransferErrorLogDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsTransferErrorLogEntity>
    {
        List<EsTransferErrorLogEntity> FindListByScheduleCode(string scheduleCycleCode);
        void DeleteOldLog(int days);
    }
}
