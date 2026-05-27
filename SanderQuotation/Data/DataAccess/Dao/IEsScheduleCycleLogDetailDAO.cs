using Core.Utility.Base.Data;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsScheduleCycleLogDetailDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleLogDetailEntity>
    {
        void DeleteOldLog(int days);

        /// <summary>
        /// 以 EsScheduleCycleLogDetail 為主表，JOIN EsScheduleCycleLog 查詢 ScheduleCycleCode，並加上日期區間篩選
        /// </summary>
        List<EsScheduleCycleLogDetailWithLogDTO> GetDetailsByCode(string scheduleCycleCode, DateTime? dateFrom, DateTime? dateTo);
    }
}
