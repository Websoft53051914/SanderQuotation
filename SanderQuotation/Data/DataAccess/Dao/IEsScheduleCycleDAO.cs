using CommonClass.Model;
using Core.Utility.Base.Data;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsScheduleCycleDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleEntity>
    {
        PageResult<EsScheduleCycleDTO> GetPageList(PageEntity pageEntity, EsScheduleCycleDTO dto);
    }
    public interface IEsScheduleCycleWeekDayDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleWeekDayEntity>
    {
        List<EsScheduleCycleWeekDayEntity> GetByCodes(IEnumerable<string> codes);
    }
    public interface IEsScheduleCycleMonthDayDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleMonthDayEntity>
    {
        List<EsScheduleCycleMonthDayEntity> GetByCodes(IEnumerable<string> codes);
    }
    public interface IEsScheduleCycleDbTransferDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleDbTransferEntity>
    {
        List<EsScheduleCycleDbTransferEntity> GetByCodes(IEnumerable<string> codes);
    }
    public interface IEsScheduleCycleDbCsvTransferDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleDbCsvTransferEntity>
    {
        List<EsScheduleCycleDbCsvTransferEntity> GetByCodes(IEnumerable<string> codes);
    }
    public interface IEsScheduleCycleExcelDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleFileTransferEntity>
    {
        List<EsScheduleCycleFileTransferEntity> GetByCodes(IEnumerable<string> codes);
    }
}
