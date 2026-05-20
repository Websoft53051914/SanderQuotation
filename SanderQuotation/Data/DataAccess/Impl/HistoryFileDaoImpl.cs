using Const;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
