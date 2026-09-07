using CommonClass.Model;
using Const;
using Core.Utility.Enums;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class EsFileTransferMappingDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsFileTransferMappingEntity>, IEsFileTransferMappingDAO
    {
        public PageResult<EsFileTransferMappingDTO> GetPageList(PageEntity pageEntity, EsFileTransferMappingDTO condition)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(condition.Keyword1))
            {
                whereSQL += $" AND ( m.transferMappingCode LIKE @Keyword1 OR m.{nameof(EsFileTransferMappingEntity.ExampleFileName)} LIKE @Keyword1 ) ";
                paras.Add("@Keyword1", $"%{condition.Keyword1}%");
            }

            whereSQL += $" AND m.{nameof(EsFileTransferMappingEntity.Status)} = @Status ";
            paras.Add("@Status", StatusEnum.Enabled.ToInt());

            string sql = $@"SELECT m.*
FROM EsFileTransferMapping m
WHERE 1=1 {whereSQL}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<EsFileTransferMappingDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, nameof(EsFileTransferMappingEntity.UpdatedAt));
        }

        public PageResult<EsFileTransferMappingDTO> GetPageList(PageEntity pageEntity,SearchVO searchVO)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                whereSQL += $@" AND (
                m.{nameof(EsFileTransferMappingEntity.TransferMappingCode)} LIKE @Keyword
                OR m.{nameof(EsFileTransferMappingEntity.ExampleFileName)} LIKE @Keyword
                OR m.{nameof(EsFileTransferMappingEntity.Description)} LIKE @Keyword
                OR EXISTS (
                    SELECT 1 FROM EsFileTransferMappingColumn c
                    WHERE c.TransferMappingCode = m.TransferMappingCode
                      AND (c.{nameof(EsFileTransferMappingColumnEntity.TargetTableName)} LIKE @Keyword OR c.{nameof(EsFileTransferMappingColumnEntity.TargetTableNameComment)} LIKE @Keyword)
                )
            ) ";
                paras.Add("@Keyword", $"%{searchVO.KeywordLike}%");
            }

            whereSQL += $" AND m.{nameof(EsFileTransferMappingEntity.Status)} = @Status ";
            paras.Add("@Status", Status.Enable.ToInt());

            string sql = $@"SELECT m.*,
    (
        SELECT STRING_AGG(
    DISTINCT COALESCE(et.DbName || '.', '') 
    || c2.{nameof(EsFileTransferMappingColumnEntity.TargetTableName)}
    || COALESCE('(' || NULLIF(c2.{nameof(EsFileTransferMappingColumnEntity.TargetTableNameComment)},'') || ')', '')
    , ','
)
FROM EsFileTransferMappingColumn c2
LEFT JOIN ESDbTransfer et ON et.TransferCode = c2.{nameof(EsFileTransferMappingColumnEntity.DBTransferMappingCode)}
WHERE c2.{nameof(EsFileTransferMappingColumnEntity.TransferMappingCode)} = m.{nameof(EsFileTransferMappingEntity.TransferMappingCode)} AND c2.{nameof(EsFileTransferMappingColumnEntity.Status)} = @Status
    ) AS {nameof(EsFileTransferMappingDTO.MappingTables)}
FROM EsFileTransferMapping m
WHERE 1=1 {whereSQL}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<EsFileTransferMappingDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, $"{pageEntity.Sort} {pageEntity.Asc},Id");
        }

        public List<EsFileTransferMappingDTO> GetList(EsFileTransferMappingDTO condition)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

            whereSQL += $" AND m.{nameof(EsFileTransferMappingEntity.Status)} = @Status ";
            paras.Add("@Status", StatusEnum.Enabled.ToInt());

            string sql = $@"SELECT m.*
FROM EsFileTransferMapping m
WHERE 1=1 {whereSQL}
ORDER BY m.{nameof(EsFileTransferMappingEntity.SortNo)}";

            return DbHelper.FindList<EsFileTransferMappingDTO>(sql, paras);
        }

        /// <summary>
        /// 取得所有匯入規則並包含 IsBomFileRule 標記
        /// （判斷依據：EsFileTransferMappingColumn 下是否存在 TargetTableName == 'bomfilecontent'）
        /// </summary>
        public List<EsFileTransferMappingDTO> GetListWithBomFlag(SearchVO searchVO)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            condition.Append("AND m.Status = @Status ");
            paras.Add("@Status", (int)Status.Enable);

            if (!string.IsNullOrWhiteSpace(searchVO.TransferMappingCodeEq))
            {
                condition.Append($"AND m.{nameof(EsFileTransferMappingEntity.TransferMappingCode)} = @{nameof(searchVO.TransferMappingCodeEq)} ");
                paras.Add(nameof(searchVO.TransferMappingCodeEq), $"{searchVO.TransferMappingCodeEq}");
            }
            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND m.Id = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq.Value);
            }

            string sql = $@"
SELECT m.*,
    CASE WHEN EXISTS (
        SELECT 1
        FROM EsFileTransferMappingColumn c
        WHERE c.TransferMappingCode = m.TransferMappingCode
          AND LOWER(c.TargetTableName) = 'bomfilecontent'
    ) THEN 1 ELSE 0 END AS IsBomFileRule
FROM EsFileTransferMapping m
WHERE 1 = 1
{condition}
ORDER BY m.TransferMappingCode ";

            return DbHelper.FindList<EsFileTransferMappingDTO>(sql, paras);
        }
    }
}
