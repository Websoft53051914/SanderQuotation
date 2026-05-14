using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;

namespace Data.DataAccess.Impl
{
    /// <summary>
    /// 內部採購紀錄 DAO 實作
    /// </summary>
    public class SanderModulePurchaseLineDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<SanderModulePurchaseLineEntity>, ISanderModulePurchaseLineDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<SanderModulePurchaseLineDTO> GetListByFilter(SearchVO searchVO)
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
                condition.Append($"AND s.{nameof(SanderModulePurchaseLineEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.SanderModuleItemNoEq))
            {
                condition.Append($"AND s.\"no\" = @{nameof(searchVO.SanderModuleItemNoEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemNoEq), searchVO.SanderModuleItemNoEq);
            }

            string sql = $@"
SELECT s.*
FROM sandermodulepurchaseline s
WHERE 1=1
{condition}
{sqlLimit}";

            return DbHelper.FindList<SanderModulePurchaseLineDTO>(sql, paras);
        }

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁清單資料</returns>
        public PageResult<SanderModulePurchaseLineDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (!string.IsNullOrWhiteSpace(searchVO.SanderModuleItemNoEq))
            {
                condition.Append($"AND s.\"no\" = @{nameof(searchVO.SanderModuleItemNoEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemNoEq), searchVO.SanderModuleItemNoEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                condition.Append($@" AND (s.{nameof(SanderModulePurchaseLineEntity.BuyFromVendorNo)} ILIKE @{nameof(searchVO.KeywordLike)}
OR s.{nameof(SanderModulePurchaseLineEntity.BuyFromVendorName)} ILIKE @{nameof(searchVO.KeywordLike)}
OR s.{nameof(SanderModulePurchaseLineEntity.Description2)} ILIKE @{nameof(searchVO.KeywordLike)}
) ");
                paras.Add(nameof(searchVO.KeywordLike), $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"
SELECT s.*
FROM sandermodulepurchaseline s
WHERE 1=1
{condition}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<SanderModulePurchaseLineDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras,
                string.IsNullOrWhiteSpace(pageEntity.Sort)
                    ? $"{nameof(SanderModulePurchaseLineEntity.DocumentDate)} DESC"
                    : $"{pageEntity.Sort} {pageEntity.Asc}");
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
                condition.Append($"AND s.{nameof(SanderModulePurchaseLineEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }

            ArgumentNullException.ThrowIfNull(condition);

            string sql = $@"
DELETE FROM sandermodulepurchaseline
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }
    }
}
