using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;

namespace Data.DataAccess.Impl
{
    /// <summary>
    /// ERP 料號基本資料 DAO 實作
    /// </summary>
    public class SanderModuleItemDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<SanderModuleItemEntity>, ISanderModuleItemDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<SanderModuleItemDTO> GetListByFilter(SearchVO searchVO)
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
                condition.Append($"AND s.{nameof(SanderModuleItemEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.SanderModuleItemNoEq))
            {
                condition.Append($"AND s.\"no\" = @{nameof(searchVO.SanderModuleItemNoEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemNoEq), searchVO.SanderModuleItemNoEq);
            }

            string sql = $@"
SELECT s.*
FROM sandermoduleitem s
WHERE 1=1
{condition}
{sqlLimit}";

            return DbHelper.FindList<SanderModuleItemDTO>(sql, paras);
        }

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁清單資料</returns>
        public PageResult<SanderModuleItemDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                condition.Append($@" AND (s.no ILIKE @{nameof(searchVO.KeywordLike)} 
OR s.{nameof(SanderModuleItemEntity.Description)} ILIKE @{nameof(searchVO.KeywordLike)} 
OR s.{nameof(SanderModuleItemEntity.Description2)} ILIKE @{nameof(searchVO.KeywordLike)}
) ");
                paras.Add(nameof(searchVO.KeywordLike), $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"
SELECT s.*
FROM sandermoduleitem s
WHERE 1=1
{condition}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<SanderModuleItemDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras,
                string.IsNullOrWhiteSpace(pageEntity.Sort)
                    ? $"\"no\" ASC"
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
                condition.Append($"AND s.{nameof(SanderModuleItemEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if (searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND s.{nameof(SanderModuleItemEntity.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }

            ArgumentNullException.ThrowIfNull(condition);

            string sql = $@"
DELETE FROM sandermoduleitem s
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }
    }
}
