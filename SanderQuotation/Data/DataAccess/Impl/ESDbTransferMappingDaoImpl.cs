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
