using CommonClass.Model;
using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsScheduleCycleDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleEntity>
    {
        PageResult<EsScheduleCycleEntity> GetPageList(PageEntity pageEntity,SearchVO searchVO);
        /// <summary>
        /// 取得符合條件的排程週期列表
        /// </summary>
        /// <param name="searchVO"></param>
        /// <returns></returns>
        List<EsScheduleCycleEntity> GetListByFilter(SearchVO searchVO);
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
        /// <summary>
        /// 取得符合條件的排程週期列表
        /// </summary>
        /// <param name="searchVO"></param>
        /// <returns></returns>
        List<EsScheduleCycleDbTransferEntity> GetListByFilter(SearchVO searchVO);
    }
    public interface IEsScheduleCycleExcelDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsScheduleCycleFileTransferEntity>
    {
        List<EsScheduleCycleFileTransferEntity> GetByCodes(IEnumerable<string> codes);
        /// <summary>
        /// 取得符合條件的排程週期列表
        /// </summary>
        /// <param name="searchVO"></param>
        /// <returns></returns>
        List<EsScheduleCycleFileTransferEntity> GetListByFilter(SearchVO searchVO);
    }
}
