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
        /// description 標記為已停用／作廢的 SQL 條件（別名 s）
        /// </summary>
        private const string DeactivatedDescriptionSql = @"
(
    COALESCE(s.description,'') ILIKE '%已停用%'
    OR COALESCE(s.description,'') ILIKE '%作廢%'
)";

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
            if (searchVO.SanderModuleItemFlagNeedExtractKeywordEq.HasValue)
            {
                condition.Append($"AND s.{nameof(SanderModuleItemEntity.FlagNeedExtractKeyword)} = @{nameof(searchVO.SanderModuleItemFlagNeedExtractKeywordEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemFlagNeedExtractKeywordEq), searchVO.SanderModuleItemFlagNeedExtractKeywordEq.Value);
            }
            if (searchVO.LimitRows.HasValue)
            {
                sqlLimit = $" LIMIT {searchVO.LimitRows.Value} ";
            }
            if (searchVO.ExcludeDeactivatedSanderModuleItem)
            {
                condition.Append($"AND NOT {DeactivatedDescriptionSql} ");
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

        /// <summary>
        /// 批次更新 FlagNeedExtractKeyword 旗標
        /// </summary>
        /// <param name="ids">要更新的資料代號清單</param>
        /// <param name="value">目標旗標值（0=否, 1=待處理, 2=錯誤）</param>
        public void UpdateFlagNeedExtractKeyword(List<Guid> ids, int value)
        {
            if (ids.Count == 0)
                return;

            Dictionary<string, object> paras = [];
            paras.Add("ids", ids.ToArray());
            paras.Add("value", value);

            string sql = $@"
UPDATE sandermoduleitem
SET {nameof(SanderModuleItemEntity.FlagNeedExtractKeyword)} = @value
WHERE {nameof(SanderModuleItemEntity.Id)} = ANY(@ids)";

            DbHelper.Execute(sql, paras);
        }

        /// <summary>
        /// 將所有 FlagNeedExtractKeyword = 2（錯誤）的料品重置為 1（待處理）
        /// </summary>
        public void ResetErrorFlagToNeedProcess()
        {
            string sql = $@"
UPDATE sandermoduleitem
SET {nameof(SanderModuleItemEntity.FlagNeedExtractKeyword)} = 1
WHERE {nameof(SanderModuleItemEntity.FlagNeedExtractKeyword)} = 2";

            DbHelper.Execute(sql, []);
        }

        /// <summary>
        /// 將 description 為已停用／作廢的料品標記為不需 AI 關鍵字抽取（flag=0）
        /// </summary>
        public void SkipDeactivatedItemsForExtractKeyword()
        {
            string sql = $@"
UPDATE sandermoduleitem s
SET {nameof(SanderModuleItemEntity.FlagNeedExtractKeyword)} = 0
WHERE s.{nameof(SanderModuleItemEntity.FlagNeedExtractKeyword)} IN (1, 2)
AND {DeactivatedDescriptionSql}";

            DbHelper.Execute(sql, []);
        }
    }
}
