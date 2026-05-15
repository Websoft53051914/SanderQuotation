using Const;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;

namespace Data.DataAccess.Impl
{
    /// <summary>
    /// 內部料號關鍵字資料模型 DAO 實作
    /// </summary>
    public class TBSanderModuleItemKeywordDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TBSanderModuleItemKeywordEntity>, ITBSanderModuleItemKeywordDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<TBSanderModuleItemKeywordDTO> GetListByFilter(SearchVO searchVO)
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
                condition.Append($"AND k.{nameof(TBSanderModuleItemKeywordEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($"AND k.{nameof(TBSanderModuleItemKeywordEntity.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.SanderModuleItemNoEq))
            {
                condition.Append($"AND k.\"no\" = @{nameof(searchVO.SanderModuleItemNoEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemNoEq), searchVO.SanderModuleItemNoEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.SanderModuleItemKeywordColumnNameEq))
            {
                condition.Append($"AND k.{nameof(TBSanderModuleItemKeywordEntity.ColumnName)} = @{nameof(searchVO.SanderModuleItemKeywordColumnNameEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemKeywordColumnNameEq), searchVO.SanderModuleItemKeywordColumnNameEq);
            }

            string sql = $@"
SELECT k.no
    , k.columnname
    , k.keyword
FROM tb_sandermoduleitemkeyword k
WHERE 1=1
{condition}
ORDER BY k.no
{sqlLimit}";

            return DbHelper.FindList<TBSanderModuleItemKeywordDTO>(sql, paras);
        }

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        public void DeleteByFilter(SearchVO searchVO, string account)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND k.{nameof(TBSanderModuleItemKeywordEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if (searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND k.{nameof(TBSanderModuleItemKeywordEntity.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.SanderModuleItemNoEq))
            {
                condition.Append($"AND k.\"no\" = @{nameof(searchVO.SanderModuleItemNoEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemNoEq), searchVO.SanderModuleItemNoEq);
            }
            if (searchVO.SanderModuleItemNoIn?.Count > 0)
            {
                condition.Append($"AND k.\"no\" = ANY(@{nameof(searchVO.SanderModuleItemNoIn)}) ");
                paras.Add(nameof(searchVO.SanderModuleItemNoIn), searchVO.SanderModuleItemNoIn.ToArray());
            }

            ArgumentNullException.ThrowIfNull(condition);

            string sql = $@"
DELETE FROM tb_sandermoduleitemkeyword k
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }

        /// <summary>
        /// 以文字相似度比對 LongDesc 關鍵字（用於 MPN / 廠牌比對）
        /// </summary>
        /// <param name="pKeyword">搜尋字串</param>
        /// <param name="pNo">限定料號（可為 null）</param>
        public List<TBSanderModuleItemKeywordDTO> GetListMatchLongDesc(string pKeyword, string? pNo)
        {
            Dictionary<string, object> paras = [];
            paras.Add(nameof(pKeyword), pKeyword);

            string noCondition = string.Empty;
            if (!string.IsNullOrEmpty(pNo))
            {
                noCondition = $"AND k.\"no\" = @{nameof(pNo)} ";
                paras.Add(nameof(pNo), pNo);
            }

            string sql = $@"
SELECT
    k.no
    , k.columnname
    , k.keyword
    , similarity(k.keyword, @pKeyword) AS SimilarityScore
FROM tb_sandermoduleitemkeyword k
WHERE 1=1
    AND (k.columnname = 'LongDesc' OR k.columnname = 'LongDesc2')
    AND similarity(k.keyword, @pKeyword) > 0.4
{noCondition}
ORDER BY similarity(k.keyword, @pKeyword) DESC, k.no
LIMIT 5";

            return DbHelper.FindList<TBSanderModuleItemKeywordDTO>(sql, paras);
        }

        /// <summary>
        /// 以向量相似度比對 Description 關鍵字
        /// </summary>
        /// <param name="pDescriptionVector">向量字串，格式 [x,y,...]</param>
        public List<TBSanderModuleItemKeywordDTO> GetListMatchDescription(string pDescriptionVector)
        {
            Dictionary<string, object> paras = [];
            paras.Add(nameof(pDescriptionVector), pDescriptionVector);

            string sql = $@"
SELECT
    k.no
    , k.columnname
    , k.keyword
    , (1 - (keywordembedding <=> @pDescriptionVector::vector)) AS SimilarityScore
FROM tb_sandermoduleitemkeyword k
WHERE 1=1
    AND (k.columnname = 'Description' OR k.columnname = 'Description2')
    AND (1 - (keywordembedding <=> @pDescriptionVector::vector)) > 0.9
ORDER BY (1 - (keywordembedding <=> @pDescriptionVector::vector)) DESC, k.no
LIMIT 5";

            return DbHelper.FindList<TBSanderModuleItemKeywordDTO>(sql, paras);
        }
    }
}
