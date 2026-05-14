using Const;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// 系統設定 DAO 介面
    /// </summary>
    public interface ITBSysSettingDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TBSysSettingEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<TBSysSettingDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        void DeleteByFilter(SearchVO searchVO, string account);
    }
}
