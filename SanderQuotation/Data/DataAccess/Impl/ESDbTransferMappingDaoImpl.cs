using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{
    public class ESDbTransferMappingDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<ESDbTransferMappingEntity>, IESDbTransferMappingDAO
    {
        public PageResult<ESDbTransferMappingDTO> FindPageList(PageEntity pageEntity, ESDbTransferMappingDTO dto)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

            if (dto.Status != null)
            {
                whereSQL += $" AND m.{nameof(ESDbTransferMappingEntity.Status)} = @Status ";
                paras.Add("@Status", dto.Status);
            }

            string sql = $@"SELECT m.*
FROM ESDbTransferMapping m
WHERE 1=1 {whereSQL}";

            string countSQL = $@"
SELECT COUNT(0) AS RowNum
FROM (
{sql}
) AS pageData
WHERE 1=1";

            return DbHelper.FindPageList<ESDbTransferMappingDTO>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, nameof(ESDbTransferMappingEntity.UpdatedAt));
        }
    }
}
