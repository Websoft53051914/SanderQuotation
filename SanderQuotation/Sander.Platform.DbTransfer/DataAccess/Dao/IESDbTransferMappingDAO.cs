using Const;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IESDbTransferMappingDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<ESDbTransferMappingEntity>
    {
        PageResult<ESDbTransferMappingDTO> FindPageList(PageEntity pageEntity, SearchVO searchVO);

        List<ESDbTransferMappingEntity> FindListByFilter(SearchVO searchVO);
    }
}
