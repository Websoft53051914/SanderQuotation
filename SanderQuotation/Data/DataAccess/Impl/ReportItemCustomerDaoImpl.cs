using Const;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;

namespace Data.DataAccess.Impl
{
    public class ReportItemCustomerDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<ReportItemCustomerEntity>, IReportItemCustomerDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<ReportItemCustomerDTO> GetListByFilter(SearchVO searchVO)
        {
            StringBuilder condition = new();
            string sqlLimit = string.Empty;
            Dictionary<string, object> paras = [];

            if (searchVO.IsLimit1)
            {
                sqlLimit = " LIMIT 1 ";
            }
            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND r.{nameof(ReportItemCustomerEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.CustomerCodeEq))
            {
                condition.Append($"AND r.{nameof(ReportItemCustomerEntity.CustomerCode)} = @{nameof(searchVO.CustomerCodeEq)} ");
                paras.Add(nameof(searchVO.CustomerCodeEq), searchVO.CustomerCodeEq);
            }

            string sql = $@"
SELECT r.*
FROM reportitemcustomer r
WHERE 1=1
{condition}
{sqlLimit}";

            return DbHelper.FindList<ReportItemCustomerDTO>(sql, paras);
        }

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        public void DeleteByFilter(SearchVO searchVO, string account)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND r.{nameof(ReportItemCustomerEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }

            ArgumentNullException.ThrowIfNull(condition);

            string sql = $@"
DELETE FROM reportitemcustomer
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }
    }
}
