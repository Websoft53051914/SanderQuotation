using Core.Utility.Base.Data;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsFileTransferMappingColumnDAO : IBaseDAO<EsFileTransferMappingColumnEntity>
    {
        List<EsFileTransferMappingColumnDTO> GetListByMappingSettingId(string mappingSettingId);
    }
}
