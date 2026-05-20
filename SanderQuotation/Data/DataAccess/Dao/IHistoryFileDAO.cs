using Const;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Dao
{
    public interface IHistoryFileDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<HistoryFileEntity>
    {
        List<HistoryFileEntity> GetListByFilter(SearchVO searchVO);
    }
}
