using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// 內部料號關鍵字資料模型 DAO 介面
    /// </summary>
    public interface ITBSanderModuleItemKeywordDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TBSanderModuleItemKeywordEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<TBSanderModuleItemKeywordDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        void DeleteByFilter(SearchVO searchVO, string account);

        /// <summary>
        /// 以文字相似度比對 LongDesc 關鍵字（用於 MPN / 廠牌比對）
        /// </summary>
        /// <param name="pKeyword">搜尋字串</param>
        /// <param name="pNo">限定料號（可為 null）</param>
        List<TBSanderModuleItemKeywordDTO> GetListMatchLongDesc(string pKeyword, string? pNo);

        /// <summary>
        /// 以向量相似度比對 Description 關鍵字
        /// </summary>
        /// <param name="pDescriptionVector">向量字串，格式 [x,y,...]</param>
        List<TBSanderModuleItemKeywordDTO> GetListMatchDescription(string pDescriptionVector);
    }
}
