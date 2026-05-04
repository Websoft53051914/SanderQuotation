using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using DocumentFormat.OpenXml.Presentation;

namespace Data.DataAccess.Impl
{
    public class ESDbTransferDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<ESDbTransferEntity>, IESDbTransferDAO
    {
        public PageResult<ESDbTransferDTO> FindPageList(PageEntity pageEntity, ESDbTransferDTO dto)
        {
            string whereSQL = string.Empty;
            var paras = new Dictionary<string, object>();

            if (dto.Status != null)
            {
                whereSQL += $" AND m.{nameof(ESDbTransferEntity.Status)} = @Status ";
                paras.Add("@Status", dto.Status);
            }

            string originSQL = $@"

SELECT m.*
FROM ESDbTransfer m
WHERE 1=1 {whereSQL}

";

            string qrySQL = @"

  SELECT  pageData.*
  FROM 
  (
" + originSQL + @"
) as pageData  
 where 1=1 

";

            string countSQL = @"
  SELECT  
    count(0)
  FROM 
  (
" + originSQL + @"
) as pageData
 where 1=1 
";


            return DbHelper.FindPageList<ESDbTransferDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, nameof(ESDbTransferEntity.UpdatedAt));
        }
    }
}
