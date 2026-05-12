using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsFileTransferUploadDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsFileTransferUploadEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        List<EsFileTransferUploadDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        PageResult<EsFileTransferUploadDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        void DeleteByFilter(SearchVO searchVO, string account);
    }
}
