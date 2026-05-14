using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// 各料品 Variant 資料 DAO 介面
    /// </summary>
    public interface ISanderModuleItemVariantDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<SanderModuleItemVariantEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<SanderModuleItemVariantDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        void DeleteByFilter(SearchVO searchVO, string account);
    }
}
