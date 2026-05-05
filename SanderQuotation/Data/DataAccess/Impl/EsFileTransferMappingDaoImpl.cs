using CommonClass.Model;
using Const;
using Core.Utility.Base.Data;
using Core.Utility.Enums;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{
    public class EsFileTransferMappingDaoImpl : BaseImpl<EsFileTransferMappingEntity>, IEsFileTransferMappingDAO
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

            if (!string.IsNullOrWhiteSpace(condition.Status))
            {
                whereSQL += $" AND m.{nameof(EsFileTransferMappingEntity.Status)} = @Status ";
                paras.Add("@Status", condition.Status);
            }

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

        public PageResult<EsFileTransferMappingDTO> GetPageList(CommonSearchQuery query)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

//            if (!string.IsNullOrWhiteSpace(query.Keyword1))
//            {
//                whereSQL += $@" AND (
//    m.{nameof(EsFileTransferMappingEntity.TransferMappingCode)} LIKE @Keyword1
//    OR m.{nameof(EsFileTransferMappingEntity.ExampleFileType)} LIKE @Keyword1
//    OR m.{nameof(EsFileTransferMappingEntity.SrcNasFilePath)} LIKE @Keyword1
//    OR m.{nameof(EsFileTransferMappingEntity.Description)} LIKE @Keyword1
//    OR EXISTS (
//        SELECT 1 FROM EsFileTransferMappingColumn c
//        WHERE c.TransferMappingCode = m.TransferMappingCode
//          AND c.{nameof(EsFileTransferMappingColumnEntity.TargetTableName)} LIKE @Keyword1
//    )
//) ";
//                paras.Add("@Keyword1", $"%{query.Keyword1}%");
//            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                whereSQL += $" AND m.{nameof(EsFileTransferMappingEntity.Status)} = @Status ";
                paras.Add("@Status", Status.Enable.ToValueString());
            }

            string sql = $@"SELECT m.*,
    STUFF((
        SELECT DISTINCT ',' + ISNULL(et.{nameof(ESDbTransferEntity.DbName)} + '.', '') + c2.{nameof(EsFileTransferMappingColumnEntity.TargetTableName)}
        FROM EsFileTransferMappingColumn c2
        LEFT JOIN ESDbTransfer et ON et.{nameof(ESDbTransferEntity.TransferCode)} = c2.{nameof(EsFileTransferMappingColumnEntity.DBTransferMappingCode)}
        WHERE c2.TransferMappingCode = m.TransferMappingCode
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') AS {nameof(EsFileTransferMappingDTO.MappingTables)}
FROM EsFileTransferMapping m
WHERE 1=1 {whereSQL}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            var orderBy = string.IsNullOrWhiteSpace(query.SortField) || query.SortField == "No"
                ? $"m.{nameof(EsFileTransferMappingEntity.UpdatedAt)} DESC"
                : $"{query.SortField} {query.SortDir}";

            return DbHelper.FindPageList<EsFileTransferMappingDTO>(sql, countSQL, query.Page, query.PageSize, paras, orderBy);
        }

        public List<EsFileTransferMappingDTO> GetList(EsFileTransferMappingDTO condition)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(condition.Status))
            {
                whereSQL += $" AND m.{nameof(EsFileTransferMappingEntity.Status)} = @Status ";
                paras.Add("@Status", condition.Status);
            }

            string sql = $@"SELECT m.*
FROM EsFileTransferMapping m
WHERE 1=1 {whereSQL}
ORDER BY m.{nameof(EsFileTransferMappingEntity.SortNo)}";

            return DbHelper.FindList<EsFileTransferMappingDTO>(sql, paras);
        }
    }
}
