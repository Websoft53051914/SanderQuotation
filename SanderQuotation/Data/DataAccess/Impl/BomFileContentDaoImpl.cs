using Const;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;

namespace Data.DataAccess.Impl
{
    /// <summary>
    /// BOM 表內容 DAO 實作
    /// </summary>
    public class BomFileContentDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<BomFileContentEntity>, IBomFileContentDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<BomFileContentDTO> GetListByFilter(SearchVO searchVO)
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
                condition.Append($"AND b.{nameof(BomFileContentEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.UploadIdEq.HasValue)
            {
                condition.Append($"AND b.{nameof(BomFileContentEntity.UploadId)} = @{nameof(searchVO.UploadIdEq)} ");
                paras.Add(nameof(searchVO.UploadIdEq), searchVO.UploadIdEq);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                condition.Append($"AND (b.{nameof(BomFileContentEntity.ComponentPart)} ILIKE @{nameof(searchVO.KeywordLike)} OR b.{nameof(BomFileContentEntity.ManufacturerPartNumber)} ILIKE @{nameof(searchVO.KeywordLike)}) ");
                paras.Add(nameof(searchVO.KeywordLike), $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"
SELECT b.*
FROM bomfilecontent b
WHERE 1=1
{condition}
{sqlLimit}";

            return DbHelper.FindList<BomFileContentDTO>(sql, paras);
        }

        /// <summary>
        /// 依條件查詢 BOM 料項及其查價結果（LEFT JOIN tbbomfilequotation）
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>聯合查詢清單</returns>
        public List<BomFileContentDTO> GetListWithQuotationByFilter(SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.UploadIdEq.HasValue)
            {
                condition.Append($"AND bfc.{nameof(BomFileContentEntity.UploadId)} = @{nameof(searchVO.UploadIdEq)} ");
                paras.Add(nameof(searchVO.UploadIdEq), searchVO.UploadIdEq);
            }
            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND bfc.{nameof(BomFileContentEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }

            string sql = $@"
SELECT bfc.*
, bfq.Id AS QuotationId
, bfq.no AS No
, bfq.InternalPurchaseOrderDate
, bfq.InternalUnitPriceOriginalCurrency
, bfq.InternalUnitPriceTwd
, bfq.InternalQuantity
, bfq.InternalCurrency
, bfq.InternalSupplierName
, bfq.InternalLowMinPrice
, bfq.InternalLowMaxPrice
, bfq.InternalHighMinPrice
, bfq.InternalHighMaxPrice
, bfq.ExternalQuotationDate
, bfq.ExternalUnitPriceOriginalCurrency
, bfq.ExternalUnitPriceTwd
, bfq.ExternalMoq
, bfq.ExternalCurrency
, bfq.ExternalSupplierName
, bfq.IsRecommendedNo
, bfq.IsFilterByCustomerApprovedPart
, bfq.CustomerApprovedPartCsv
FROM bomfilecontent bfc
LEFT JOIN tb_bomfilequotation bfq ON bfq.BomFileContentId = bfc.Id
WHERE 1=1
{condition}
ORDER BY bfc.Id";

            return DbHelper.FindList<BomFileContentDTO>(sql, paras);
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
                condition.Append($"AND b.{nameof(BomFileContentEntity.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if (searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND b.{nameof(BomFileContentEntity.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }
            if (searchVO.UploadIdEq.HasValue)
            {
                condition.Append($"AND b.{nameof(BomFileContentEntity.UploadId)} = @{nameof(searchVO.UploadIdEq)} ");
                paras.Add(nameof(searchVO.UploadIdEq), searchVO.UploadIdEq);
            }

            ArgumentNullException.ThrowIfNull(condition);

            string sql = $@"
DELETE FROM bomfilecontent b
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }
    }
}
