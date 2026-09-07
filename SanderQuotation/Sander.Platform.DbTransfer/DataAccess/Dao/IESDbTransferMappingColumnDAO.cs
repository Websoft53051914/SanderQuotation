using Const;
using Core.Utility.Base.Data;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IESDbTransferMappingColumnDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<ESDbTransferMappingColumnEntity>
    {
        List<ESDbTransferMappingColumnEntity> FindListByFilter(SearchVO searchVO);
    }
}
