using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;
using static Const.Enums;

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
            string orderBy = $"u.{nameof(EsFileTransferUploadDTO.CreatedAt)}";
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
            if (searchVO.ProcessStatusEq.HasValue)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.ProcessStatus)} = @{nameof(searchVO.ProcessStatusEq)} ");
                paras.Add(nameof(searchVO.ProcessStatusEq), searchVO.ProcessStatusEq);
            }
            if (searchVO.EsFileTransferMappingIdEq.HasValue)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.EsFileTransferMappingId)} = @{nameof(searchVO.EsFileTransferMappingIdEq)} ");
                paras.Add(nameof(searchVO.EsFileTransferMappingIdEq), searchVO.EsFileTransferMappingIdEq);
            }
            if (searchVO.EsFileTransferMappingIdIn.Count > 0)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.EsFileTransferMappingId)} = ANY(@{nameof(searchVO.EsFileTransferMappingIdIn)}) ");
                paras.Add(nameof(searchVO.EsFileTransferMappingIdIn), searchVO.EsFileTransferMappingIdIn.ToArray());
            }

            string sql = $@"
SELECT u.*, eftm.{nameof(EsFileTransferMappingEntity.TransferMappingCode)}
FROM EsFileTransferUpload u
LEFT JOIN EsFileTransferMapping eftm ON eftm.Id = u.EsFileTransferMappingId
WHERE 1=1
{condition}
ORDER BY {orderBy}
{sqlLimit}";

            return DbHelper.FindList<EsFileTransferUploadDTO>(sql, paras);
        }

        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        public PageResult<EsFileTransferUploadDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];
            // 代表上傳未儲存的資料
            condition.Append($" AND COALESCE(u.{nameof(EsFileTransferUploadDTO.ProcessStatus)}, 0) <> 0 ");

            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($" AND u.{nameof(EsFileTransferUploadDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                condition.Append($@" AND (u.{nameof(EsFileTransferUploadDTO.FileName)} ILIKE @{nameof(searchVO.KeywordLike)}
OR eftm.{nameof(EsFileTransferMappingEntity.TransferMappingCode)} ILIKE @{nameof(searchVO.KeywordLike)}
OR rc.{nameof(EsFileTransferUploadDTO.CustomerName)} ILIKE @{nameof(searchVO.KeywordLike)}
)");
                paras.Add(nameof(searchVO.KeywordLike), $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"
SELECT u.*
, eftm.TransferMappingCode
, rc.CustomerName
FROM EsFileTransferUpload u
LEFT JOIN EsFileTransferMapping eftm ON eftm.Id = u.EsFileTransferMappingId
LEFT JOIN (-- 依據 CustomerCode 去重
    SELECT DISTINCT ON (CustomerCode) CustomerCode, CustomerName
    FROM ReportItemCustomer
    ORDER BY CustomerCode) rc ON rc.CustomerCode = u.CustomerCode
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
            else if (searchVO.IdIn?.Count > 0)
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

        /// <summary>
        /// 分頁查詢清單-定時查價結果
        /// </summary>
        public PageResult<EsFileTransferUploadDTO> GetPageListQuotationResult(PageEntity pageEntity, SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];
            condition.Append($" AND COALESCE(u.{nameof(EsFileTransferUploadDTO.Status)}, 0) <> {(int)Enums.StatusEnum.Cancel} ");
            // 已完成查價的資料
            condition.Append($" AND COALESCE(u.{nameof(EsFileTransferUploadDTO.ProcessStatus)}, 0) = {(int)EsFileTransferUploadProcessStatusEnum.PricingDone} ");

            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND u.{nameof(EsFileTransferUploadDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($" AND u.{nameof(EsFileTransferUploadDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                condition.Append($@" AND (u.{nameof(EsFileTransferUploadDTO.FileName)} ILIKE @{nameof(searchVO.KeywordLike)}
OR eftm.{nameof(EsFileTransferMappingEntity.TransferMappingCode)} ILIKE @{nameof(searchVO.KeywordLike)}
OR rc.{nameof(EsFileTransferUploadDTO.CustomerName)} ILIKE @{nameof(searchVO.KeywordLike)}
)");
                paras.Add(nameof(searchVO.KeywordLike), $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"
SELECT u.*
, eftm.TransferMappingCode
, rc.CustomerName
, (SELECT COUNT(*) FROM bomfilecontent bfc WHERE bfc.UploadId = u.UploadId) AS ItemCount
FROM EsFileTransferUpload u
LEFT JOIN EsFileTransferMapping eftm ON eftm.Id = u.EsFileTransferMappingId
LEFT JOIN (-- 依據 CustomerCode 去重
    SELECT DISTINCT ON (CustomerCode) CustomerCode, CustomerName
    FROM ReportItemCustomer
    ORDER BY CustomerCode) rc ON rc.CustomerCode = u.CustomerCode
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
        /// 更新處理狀態
        /// </summary>
        /// <param name="uploadId">檔案儲存代號</param>
        /// <param name="processStatus">目標處理狀態</param>
        public void UpdateProcessStatus(Guid uploadId, int processStatus)
        {
            Dictionary<string, object> paras = [];
            paras.Add("uploadId", uploadId);
            paras.Add("processStatus", processStatus);
            paras.Add("updatedAt", DateTime.Now);

            string sql = @"
UPDATE EsFileTransferUpload
SET
    ProcessStatus = @processStatus
    , UpdatedAt = @updatedAt
WHERE
    UploadId = @uploadId";

            DbHelper.Execute(sql, paras);
        }

        /// <summary>
        /// 查詢 Status &lt;&gt; activeStatus 且 UpdatedAt &lt; updatedBefore 的記錄
        /// </summary>
        public List<EsFileTransferUploadEntity> GetOldNonActiveList(int activeStatus, DateTime updatedBefore)
        {
            string sql = "SELECT * FROM EsFileTransferUpload WHERE Status <> @ActiveStatus AND UpdatedAt < @UpdatedBefore";
            return DbHelper.FindList<EsFileTransferUploadEntity>(sql, new Dictionary<string, object>
            {
                { "ActiveStatus", activeStatus },
                { "UpdatedBefore", updatedBefore }
            });
        }
    }
}
