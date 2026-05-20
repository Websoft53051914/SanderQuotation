using Const;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class HistoryFileDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<HistoryFileEntity>, IHistoryFileDAO
    {
        public List<HistoryFileEntity> GetListByFilter(SearchVO searchVO)
        {
            Dictionary<string, object> param = new();
            string whereSQL = "WHERE 1=1";
            if (searchVO.StatusEq.HasValue)
            {
                whereSQL += " AND Status=@StatusEq";
                param.Add("StatusEq", searchVO.StatusEq.Value);
            }
            if (searchVO.UploadIdIn.Count>0)
            {   
                whereSQL += " AND UploadId = ANY(@UploadIdIn)";
                param.Add("UploadIdIn", searchVO.UploadIdIn);
            }
            if (searchVO.IdIn.Count > 0)
            {
                whereSQL += " AND Id = ANY(@IdIn)";
                param.Add("IdIn", searchVO.IdIn);
            }
            string sql = "SELECT * FROM HistoryFile " + whereSQL;
            return DbHelper.FindList<HistoryFileEntity>(sql, param);
        }

        public PageResult<HistoryFileDTO> FindPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            string whereSQL = $" AND m.{nameof(HistoryFileEntity.Status)} = @Status";
            var paras = new Dictionary<string, object>();
            paras.Add("@Status", StatusEnum.Enabled.ToInt());

            if (!string.IsNullOrEmpty(searchVO.KeywordLike))
            {
                whereSQL += $" AND m.{nameof(HistoryFileEntity.FileName)} LIKE @KeywordLike OR  m.{nameof(HistoryFileEntity.FileSummary)} LIKE @KeywordLike";
                paras.Add("@KeywordLike", "%" + searchVO.KeywordLike + "%");
            }

            string originSQL = $@"
SELECT m.*, a.accountname as UpdatedByName
FROM HistoryFile m
INNER JOIN tb_account a on a.memberaccount = m.updatedby
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


            return DbHelper.FindPageList<HistoryFileDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, $"{pageEntity.Sort} {pageEntity.Asc}, Id");
        }
    }
}
