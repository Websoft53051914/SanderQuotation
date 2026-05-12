using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;

namespace Data.DataAccess.Impl
{
    public class EsFileTransferUploadDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsFileTransferUploadEntity>, IEsFileTransferUploadDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        public List<EsFileTransferUploadDTO> GetListByFilter(SearchVO searchVO)
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
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq);
            }
            if (searchVO.UploadIdEq.HasValue)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.UploadId)} = @{nameof(searchVO.UploadIdEq)} ");
                paras.Add(nameof(searchVO.UploadIdEq), searchVO.UploadIdEq);
            }

            string sql = $@"
SELECT u.*, eftm.{nameof(EsFileTransferMappingEntity.TransferMappingCode)}
FROM EsFileTransferUpload u
LEFT JOIN EsFileTransferMapping eftm ON eftm.Id = u.{nameof(EsFileTransferUploadEntity.EsFileTransferMappingId)}
WHERE 1=1
{condition}
{sqlLimit}";

            return DbHelper.FindList<EsFileTransferUploadDTO>(sql, paras);
        }

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        public PageResult<EsFileTransferUploadDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            string condition = string.Empty;
            var paras = new Dictionary<string, object>();
            // 代表上傳未儲存的資料
            condition += $" AND COALESCE(u.{nameof(EsFileTransferUploadDTO.ProcessStatus)}, 0) <> 0 ";

            if (searchVO.StatusEq.HasValue)
            {
                condition += $" AND u.{nameof(EsFileTransferUploadDTO.Status)} = @{nameof(searchVO.StatusEq)} ";
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                condition += $@" AND (u.{nameof(EsFileTransferUploadDTO.FileName)} ILIKE @{nameof(searchVO.KeywordLike)}
OR eftm.{nameof(EsFileTransferMappingEntity.TransferMappingCode)} ILIKE @{nameof(searchVO.KeywordLike)}
OR rc.{nameof(EsFileTransferUploadDTO.CustomerName)} ILIKE @{nameof(searchVO.KeywordLike)}
)";
                paras.Add(nameof(searchVO.KeywordLike), $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"
SELECT u.*, eftm.{nameof(EsFileTransferMappingEntity.TransferMappingCode)}, rc.CustomerName
FROM EsFileTransferUpload u
LEFT JOIN EsFileTransferMapping eftm ON eftm.Id = u.{nameof(EsFileTransferUploadEntity.EsFileTransferMappingId)}
LEFT JOIN ReportItemCustomer rc ON rc.CustomerCode = u.CustomerCode
WHERE 1 = 1 
{condition}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<EsFileTransferUploadDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras,
                string.IsNullOrWhiteSpace(pageEntity.Sort)
                    ? $"{nameof(EsFileTransferUploadEntity.UpdatedAt)} DESC"
                    : $"{pageEntity.Sort} {pageEntity.Asc}");
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
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if(searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq);
            }
            if (searchVO.UploadIdEq.HasValue)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.UploadId)} = @{nameof(searchVO.UploadIdEq)} ");
                paras.Add(nameof(searchVO.UploadIdEq), searchVO.UploadIdEq);
            }

            ArgumentNullException.ThrowIfNull(condition);

            paras.Add("DeleteStatus", (int)Enums.StatusEnum.Cancel);
            paras.Add("UpdatedBy", account);
            paras.Add("UpdatedAt", DateTime.Now);

            string sql = $@"
UPDATE EsFileTransferUpload u
SET
    Status = @DeleteStatus
    , UpdatedBy = @UpdatedBy
    , UpdatedAt = @UpdatedAt
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }
    }
}
