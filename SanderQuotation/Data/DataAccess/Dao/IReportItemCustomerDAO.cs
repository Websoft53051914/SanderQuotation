using Const;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IReportItemCustomerDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<ReportItemCustomerEntity>
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        List<ReportItemCustomerDTO> GetListByFilter(SearchVO searchVO);

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        void DeleteByFilter(SearchVO searchVO, string account);
    }
}
