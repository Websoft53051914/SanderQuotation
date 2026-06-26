using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// ERP 料號基本資料 DAO 介面
    /// </summary>
    public interface ISanderModuleItemDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<SanderModuleItemEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<SanderModuleItemDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁清單資料</returns>
        PageResult<SanderModuleItemDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        void DeleteByFilter(SearchVO searchVO, string account);

        /// <summary>
        /// 批次更新 FlagNeedExtractKeyword 旗標
        /// </summary>
        /// <param name="ids">要更新的資料代號清單</param>
        /// <param name="value">目標旗標傀（0=否, 1=待處理, 2=錯誤）</param>
        void UpdateFlagNeedExtractKeyword(List<Guid> ids, int value);

        /// <summary>
        /// 將所有 FlagNeedExtractKeyword = 2（錯誤）的料品重置為 1（待處理）
        /// </summary>
        void ResetErrorFlagToNeedProcess();

        /// <summary>
        /// 將 description 為已停用／作廢的料品標記為不需 AI 關鍵字抽取（flag=0）
        /// </summary>
        void SkipDeactivatedItemsForExtractKeyword();

        /// <summary>
        /// 依客戶料號在 longdesc / longdesc2 / description2 字面比對（Step 2 fallback）
        /// </summary>
        /// <param name="partNumber">客戶料號（已正規化）</param>
        List<SanderModuleItemPartMatchDTO> GetListMatchPartNumberInSpecFields(string partNumber);
    }
}
