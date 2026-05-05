using CommonClass.Model;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsFileTransferMappingDAO : IBaseDAO<EsFileTransferMappingEntity>
    {
        PageResult<EsFileTransferMappingDTO> GetPageList(PageEntity pageEntity, EsFileTransferMappingDTO condition);

        PageResult<EsFileTransferMappingDTO> GetPageList(CommonSearchQuery query);

        List<EsFileTransferMappingDTO> GetList(EsFileTransferMappingDTO condition);
    }
}
