using Const;
using Core.Utility.Base.Data.GuidId;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsFileTransferMappingColumnDAO : IBaseDAO<EsFileTransferMappingColumnEntity>
    {
        List<EsFileTransferMappingColumnDTO> GetListByMappingSettingId(string mappingSettingId);

        List<EsFileTransferMappingColumnEntity> FindListByFilter(SearchVO searchVO);
    }
}
