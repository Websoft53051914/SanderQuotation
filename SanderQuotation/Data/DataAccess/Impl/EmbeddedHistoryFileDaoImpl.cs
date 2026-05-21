using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class EmbeddedHistoryFileDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EmbeddedHistoryFileEntity>, IEmbeddedHistoryFileDAO
    {
        public void InsertFile(EmbeddedHistoryFileEntity entity)
        {
            string sql = $@"
INSERT INTO public.embeddedhistoryfile
(id, embedding, status, historyfileid, createdby, updatedby, createdat, updatedat)
VALUES(gen_random_uuid(), @embedding::vector, {entity.Status}, @historyfileid, @createdby, @updatedby, @createdat, @updatedat);
";
            DbHelper.Execute(sql, new Dictionary<string, object>()
            {
                { "embedding", entity.Embedding },
                { "historyfileid", entity.HistoryFileId },
                { "createdby", entity.CreatedBy },
                { "updatedby", entity.UpdatedBy },
                { "createdat", entity.CreatedAt },
                { "updatedat", entity.UpdatedAt }
            });
        }

        public void UpdateFile(EmbeddedHistoryFileEntity entity)
        {
            string sql = $@"
UPDATE public.embeddedhistoryfile
SET embedding = @embedding::vector,
    updatedby = @updatedby,
    updatedat = @updatedat
WHERE historyfileid = @historyfileid;
";
            DbHelper.Execute(sql, new Dictionary<string, object>()
            {
                { "embedding", entity.Embedding },
                { "updatedby", entity.UpdatedBy },
                { "updatedat", entity.UpdatedAt },
                { "historyfileid", entity.HistoryFileId }
            });
        }

        public void DeleteFile(List<Guid> historyFileIds,string account)
        {
            string sql = $@"
UPDATE public.embeddedhistoryfile
SET status = { (int)StatusEnum.Cancel },
    updatedby = @updatedby,
    updatedat = @updatedat
WHERE historyfileid = ANY(@historyFileIds);
";
            DbHelper.Execute(sql, new Dictionary<string, object>()
            {
                { "historyFileIds", historyFileIds },
                {"updatedat", DateTime.Now },
                {"updatedby", account }
            });
        }

        public void PhysicalDeleteFile(List<Guid> historyFileIds)
        {
            string sql = $@"
DELETE FROM public.embeddedhistoryfile
WHERE historyfileid = ANY(@historyFileIds);
";
            DbHelper.Execute(sql, new Dictionary<string, object>()
            {
                { "historyFileIds", historyFileIds }
            });
        }
    }
}
