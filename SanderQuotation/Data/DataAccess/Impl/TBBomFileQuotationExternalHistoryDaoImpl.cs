using Const;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    /// <summary>
    /// BOM 外部查價歷史 DAO 實作
    /// </summary>
    public class TBBomFileQuotationExternalHistoryDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TBBomFileQuotationExternalHistoryEntity>, ITBBomFileQuotationExternalHistoryDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<TBBomFileQuotationExternalHistoryDTO> GetListByFilter(SearchVO searchVO)
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
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.ManufacturerPartNumberEq))
            {
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.ManufacturerPartNumber)} = @{nameof(searchVO.ManufacturerPartNumberEq)} ");
                paras.Add(nameof(searchVO.ManufacturerPartNumberEq), searchVO.ManufacturerPartNumberEq);
            }
            if (searchVO.QuotationDateGte.HasValue)
            {
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.QuotationDate)} >= @{nameof(searchVO.QuotationDateGte)} ");
                paras.Add(nameof(searchVO.QuotationDateGte), searchVO.QuotationDateGte.Value);
            }
            if (searchVO.QuotationDateLt.HasValue)
            {
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.QuotationDate)} < @{nameof(searchVO.QuotationDateLt)} ");
                paras.Add(nameof(searchVO.QuotationDateLt), searchVO.QuotationDateLt.Value);
            }

            string sql = $@"
SELECT h.*
FROM tb_bomfilequotationexternalhistory h
WHERE 1=1
{condition}
ORDER BY h.QuotationDate DESC, h.UpdatedAt DESC
{sqlLimit}";

            return DbHelper.FindList<TBBomFileQuotationExternalHistoryDTO>(sql, paras);
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
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if (searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.ManufacturerPartNumberEq))
            {
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.ManufacturerPartNumber)} = @{nameof(searchVO.ManufacturerPartNumberEq)} ");
                paras.Add(nameof(searchVO.ManufacturerPartNumberEq), searchVO.ManufacturerPartNumberEq);
            }
            if (searchVO.QuotationDateLt.HasValue)
            {
                condition.Append($"AND h.{nameof(TBBomFileQuotationExternalHistoryDTO.QuotationDate)} < @{nameof(searchVO.QuotationDateLt)} ");
                paras.Add(nameof(searchVO.QuotationDateLt), searchVO.QuotationDateLt.Value);
            }

            ArgumentNullException.ThrowIfNull(condition);

            string sql;
            if (isLogicalDelete)
            {
                paras.Add("DeleteStatus", (int)StatusEnum.Cancel);
                paras.Add("UpdatedBy", account);
                paras.Add("UpdatedAt", DateTime.Now);

                sql = $@"
UPDATE tb_bomfilequotationexternalhistory h
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
DELETE FROM tb_bomfilequotationexternalhistory h
WHERE
    1 = 1
{condition}";
            }

            DbHelper.Execute(sql, paras);
        }
    }
}
