using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// BOM 查價(料)結果 DAO 介面
    /// </summary>
    public interface ITBBomFileQuotationDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TBBomFileQuotationEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        List<TBBomFileQuotationDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        PageResult<TBBomFileQuotationDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        void DeleteByFilter(SearchVO searchVO, string account);
    }
}
