using Core.Utility.Base.Data;

namespace Data.UnitOfWork
{
    /// <summary>
    /// SOP 資料庫 UnitOfWork 介面（獨立連線，不干擾主資料庫）
    /// </summary>
    public interface IUnitOfWorkSOP
    {
        /// <summary>
        /// 取得指定 DAO 實例，並注入 SOP DB 連線
        /// </summary>
        T Repository<T>() where T : IBaseSuperDao;
    }
}
