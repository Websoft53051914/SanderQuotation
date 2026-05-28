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
    /// BOM 查價(料)結果 DAO 實作
    /// </summary>
    public class TBBomFileQuotationDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TBBomFileQuotationEntity>, ITBBomFileQuotationDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        public List<TBBomFileQuotationDTO> GetListByFilter(SearchVO searchVO)
        {
            StringBuilder condition = new();
            string orderBy = "q.CreatedAt DESC";
            string sqlLimit = string.Empty;
            Dictionary<string, object> paras = [];

            if (searchVO.IsLimit1)
            {
                sqlLimit = " LIMIT 1 ";
            }
            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.SanderModuleItemNoEq))
            {
                condition.Append($"AND q.\"no\" = @{nameof(searchVO.SanderModuleItemNoEq)} ");
                paras.Add(nameof(searchVO.SanderModuleItemNoEq), searchVO.SanderModuleItemNoEq);
            }
            if (searchVO.BomFileContentIdNeq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.BomFileContentId)} != @{nameof(searchVO.BomFileContentIdNeq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdNeq), searchVO.BomFileContentIdNeq);
            }
            if (searchVO.CreatedAtGte.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.CreatedAt)} >= @{nameof(searchVO.CreatedAtGte)} ");
                paras.Add(nameof(searchVO.CreatedAtGte), searchVO.CreatedAtGte.Value);
            }
            if (searchVO.InternalQuotationDateGte.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.InternalQuotationDate)} >= @{nameof(searchVO.InternalQuotationDateGte)} ");
                paras.Add(nameof(searchVO.InternalQuotationDateGte), searchVO.InternalQuotationDateGte.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.CustomerCodeEq))
            {
                condition.Append($@"AND EXISTS (
    SELECT 1 FROM bomfilecontent bc
    INNER JOIN esfiletransferupload eu ON eu.uploadid = bc.uploadid
    WHERE bc.id = q.BomFileContentId
    AND eu.customercode = @{nameof(searchVO.CustomerCodeEq)}
) ");
                paras.Add(nameof(searchVO.CustomerCodeEq), searchVO.CustomerCodeEq);
            }
            if (searchVO.OrderByColumnList?.Count > 0)
            {
                List<string> orderBySub = [];
                foreach (string item in searchVO.OrderByColumnList)
                {
                    if(item == nameof(searchVO.InternalQuotationDateOdr))
                    {
                        searchVO.InternalQuotationDateOdr ??= "ASC";
                        orderBySub.Add($"q.{nameof(TBBomFileQuotationDTO.InternalQuotationDate)} {searchVO.InternalQuotationDateOdr}");
                    }

                    orderBy = string.Join(",", orderBySub);
                }
            }

            string sql = $@"
SELECT q.*
FROM tb_bomfilequotation q
WHERE 1=1
{condition}
ORDER BY {orderBy}
{sqlLimit}";

            return DbHelper.FindList<TBBomFileQuotationDTO>(sql, paras);
        }

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        public PageResult<TBBomFileQuotationDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($" AND q.{nameof(TBBomFileQuotationDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq.Value);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($" AND q.{nameof(TBBomFileQuotationDTO.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                condition.Append($" AND q.\"no\" ILIKE @{nameof(searchVO.KeywordLike)} ");
                paras.Add(nameof(searchVO.KeywordLike), $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"
SELECT q.*
FROM tb_bomfilequotation q
WHERE 1=1
{condition}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<TBBomFileQuotationDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras,
                string.IsNullOrWhiteSpace(pageEntity.Sort)
                    ? $"{nameof(TBBomFileQuotationEntity.UpdatedAt)} DESC"
                    : $"{pageEntity.Sort} {pageEntity.Asc}");
        }

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        public void DeleteByFilter(SearchVO searchVO, string account)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if (searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($"AND q.{nameof(TBBomFileQuotationDTO.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq);
            }

            ArgumentNullException.ThrowIfNull(condition);

            paras.Add("DeleteStatus", (int)StatusEnum.Cancel);
            paras.Add("UpdatedBy", account);
            paras.Add("UpdatedAt", DateTime.Now);

            string sql = $@"
UPDATE tb_bomfilequotation q
SET
    status = @DeleteStatus
    , updatedby = @UpdatedBy
    , updatedat = @UpdatedAt
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }
    }
}
