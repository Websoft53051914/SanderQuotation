using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    /// <summary>
    /// BOM 現貨優惠價結果 DAO 實作
    /// </summary>
    public class TBBomFileQuotationOtherDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TBBomFileQuotationOtherEntity>, ITBBomFileQuotationOtherDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<TBBomFileQuotationOtherDTO> GetListByFilter(SearchVO searchVO)
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
                condition.Append($"AND q.{nameof(TBBomFileQuotationOtherEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationOtherEntity.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationOtherEntity.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq);
            }
            if (searchVO.BomFileContentIdIn.Count > 0)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationOtherEntity.BomFileContentId)} = ANY(@BomFileContentIdIn) ");
                paras.Add("BomFileContentIdIn", searchVO.BomFileContentIdIn.ToArray());
            }

            string sql = $@"
SELECT q.*
FROM tb_bomfilequotationother q
WHERE 1=1
{condition}
{sqlLimit}";

            return DbHelper.FindList<TBBomFileQuotationOtherDTO>(sql, paras);
        }

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        /// <param name="pageEntity">分頁資訊</param>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>分頁清單資料</returns>
        public PageResult<TBBomFileQuotationOtherDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($" AND q.{nameof(TBBomFileQuotationOtherEntity.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq.Value);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($" AND q.{nameof(TBBomFileQuotationOtherEntity.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq.Value);
            }

            string sql = $@"
SELECT q.*
FROM tb_bomfilequotationother q
WHERE 1=1
{condition}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<TBBomFileQuotationOtherDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras,
                string.IsNullOrWhiteSpace(pageEntity.Sort)
                    ? $"{nameof(TBBomFileQuotationOtherEntity.UpdatedAt)} DESC"
                    : $"{pageEntity.Sort} {pageEntity.Asc}");
        }

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        /// <param name="isLogicalDelete">true 為邏輯刪除 (UPDATE status)；false 為物理刪除 (DELETE)</param>
        public void DeleteByFilter(SearchVO searchVO, string account, bool isLogicalDelete = true)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationOtherDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if (searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationOtherDTO.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationOtherDTO.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq);
            }

            ArgumentNullException.ThrowIfNull(condition);

            string sql;
            if (isLogicalDelete)
            {
                paras.Add("DeleteStatus", (int)StatusEnum.Cancel);
                paras.Add("UpdatedBy", account);
                paras.Add("UpdatedAt", DateTime.Now);

                sql = $@"
UPDATE tb_bomfilequotationother q
SET
    status = @DeleteStatus
    , updatedby = @UpdatedBy
    , updatedat = @UpdatedAt
WHERE
    1 = 1
{condition}";
            }
            else
            {
                sql = $@"
DELETE FROM tb_bomfilequotationother q
WHERE
    1 = 1
{condition}";
            }

            DbHelper.Execute(sql, paras);
        }
    }
}
