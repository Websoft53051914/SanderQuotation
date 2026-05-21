using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// BOM 現貨優惠價結果 DAO 介面
    /// </summary>
    public interface ITBBomFileQuotationOtherDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TBBomFileQuotationOtherEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<TBBomFileQuotationOtherDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁清單資料</returns>
        PageResult<TBBomFileQuotationOtherDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        void DeleteByFilter(SearchVO searchVO, string account);
    }
}
