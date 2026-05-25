using Const;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// BOM 外部查價歷史 DAO 介面
    /// </summary>
    public interface ITBBomFileQuotationExternalHistoryDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TBBomFileQuotationExternalHistoryEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<TBBomFileQuotationExternalHistoryDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        /// <param name="isLogicalDelete">true 為邏輯刪除 (UPDATE status)；false 為物理刪除 (DELETE)</param>
        void DeleteByFilter(SearchVO searchVO, string account, bool isLogicalDelete = true);
    }
}
