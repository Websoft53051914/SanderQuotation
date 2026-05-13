using Const;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    /// <summary>
    /// BOM 表內容 DAO 介面
    /// </summary>
    public interface IBomFileContentDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<BomFileContentEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<BomFileContentDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 依 UploadId 查詢 BOM 料項及其查價結果（LEFT JOIN tbbomfilequotation）
        /// </summary>
        /// <param name="uploadId">EsFileTransferUpload.UploadId</param>
        /// <returns>聯合查詢清單</returns>
        List<BomFileContentQuotationDTO> GetListWithQuotationByUploadId(Guid uploadId);

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        void DeleteByFilter(SearchVO searchVO, string account);
    }
}
