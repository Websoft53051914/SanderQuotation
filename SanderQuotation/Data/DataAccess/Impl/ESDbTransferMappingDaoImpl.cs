using Const;
using Core.Utility.Base.Data;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class ESDbTransferMappingDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<ESDbTransferMappingEntity>, IESDbTransferMappingDAO
    {
        public PageResult<ESDbTransferMappingDTO> FindPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

            whereSQL += $" AND m.{nameof(ESDbTransferMappingEntity.Status)} = @Status ";
            paras.Add("@Status", StatusEnum.Enabled.ToInt());

            if (!string.IsNullOrEmpty(searchVO.KeywordLike))
            {
                whereSQL += $" AND (m.{nameof(ESDbTransferMappingEntity.TransferMappingCode)} LIKE @Keyword OR m.{nameof(ESDbTransferMappingEntity.SrcTableName)} LIKE @Keyword OR m.{nameof(ESDbTransferMappingEntity.DstTableName)} LIKE @Keyword OR EDT1.{nameof(ESDbTransferEntity.TransferName)} LIKE @Keyword OR EDT2.{nameof(ESDbTransferEntity.TransferName)} LIKE @Keyword) ";
                paras.Add("@Keyword", $"%{searchVO.KeywordLike}%");
            }

            string sql = $@"SELECT m.*, EDT1.{nameof(ESDbTransferEntity.TransferName)} AS SrcTransferName, EDT2.{nameof(ESDbTransferEntity.TransferName)} AS DstTransferName
FROM ESDbTransferMapping m
INNER JOIN ESDbTransfer EDT1 ON (m.{nameof(ESDbTransferMappingEntity.SrcDbTransferCode)} = EDT1.{nameof(ESDbTransferEntity.TransferCode)} AND EDT1.{nameof(ESDbTransferEntity.Status)} = @Status)
INNER JOIN ESDbTransfer EDT2 ON (m.{nameof(ESDbTransferMappingEntity.DstDbTransferCode)} = EDT2.{nameof(ESDbTransferEntity.TransferCode)} AND EDT2.{nameof(ESDbTransferEntity.Status)} = @Status)
WHERE 1=1 {whereSQL}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<ESDbTransferMappingDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, $"{pageEntity.Sort} {pageEntity.Asc},Id");
        }

        public List<ESDbTransferMappingEntity> FindListByFilter(SearchVO searchVO)
        {
            string whereSQL = " AND m.Status = @Status";
            var paras = new Dictionary<string, object>();
            paras.Add("@Status", StatusEnum.Enabled.ToInt());
            if (searchVO.TransferCodeIn != null && searchVO.TransferCodeIn.Count > 0)
            {
                whereSQL += $" AND (m.{nameof(ESDbTransferMappingEntity.SrcDbTransferCode)} =  ANY(@TransferCode) OR m.{nameof(ESDbTransferMappingEntity.DstDbTransferCode)} =  ANY(@TransferCode))";
                paras.Add("@TransferCode", searchVO.TransferCodeIn);
            }
            string sql = $@"SELECT m.* FROM ESDbTransferMapping m WHERE 1=1 {whereSQL}";
            return DbHelper.FindList<ESDbTransferMappingEntity>(sql, paras);
        }
    }

}
