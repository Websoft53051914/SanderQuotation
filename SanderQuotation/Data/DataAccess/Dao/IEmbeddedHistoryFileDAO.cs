using Const;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Dao
{
    public interface IEmbeddedHistoryFileDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EmbeddedHistoryFileEntity>
    {
        void InsertFile(EmbeddedHistoryFileEntity entity);
        void UpdateFile(EmbeddedHistoryFileEntity entity);

        void DeleteFile(List<Guid> historyFileIds, string account);


        void PhysicalDeleteFile(List<Guid> historyFileIds);
    }
}
