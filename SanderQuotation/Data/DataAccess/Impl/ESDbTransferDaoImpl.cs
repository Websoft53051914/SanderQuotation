using Const;
using Core.Utility.Base.Data;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using DocumentFormat.OpenXml.Presentation;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class ESDbTransferDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<ESDbTransferEntity>, IESDbTransferDAO
    {
        public PageResult<ESDbTransferDTO> FindPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            string whereSQL = $" AND m.{nameof(ESDbTransferEntity.Status)} = @Status";
            var paras = new Dictionary<string, object>();
            paras.Add("@Status", StatusEnum.Enabled.ToInt());

            if (!string.IsNullOrEmpty(searchVO.KeywordLike))
            {
                whereSQL += $" AND m.{nameof(ESDbTransferEntity.TransferCode)} LIKE @KeywordLike OR  m.{nameof(ESDbTransferEntity.TransferName)} LIKE @KeywordLike OR m.{nameof(ESDbTransferEntity.DbHost)} LIKE @KeywordLike";
                paras.Add("@KeywordLike","%"+ searchVO.KeywordLike+"%");
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


            return DbHelper.FindPageList<ESDbTransferDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, $"{pageEntity.Sort} {pageEntity.Asc}, Id");
        }
    }
}
