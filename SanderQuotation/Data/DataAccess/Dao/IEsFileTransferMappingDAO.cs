using CommonClass.Model;
using Const;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IEsFileTransferMappingDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<EsFileTransferMappingEntity>
    {
        PageResult<EsFileTransferMappingDTO> GetPageList(PageEntity pageEntity, EsFileTransferMappingDTO condition);

        PageResult<EsFileTransferMappingDTO> GetPageList(PageEntity pageEntity, SearchVO searchVO);

        List<EsFileTransferMappingDTO> GetList(EsFileTransferMappingDTO condition);

        /// <summary>
        /// 取得所有匯入規則並包含 IsBomFileRule 標記
        /// （判斷依據：EsFileTransferMappingColumn 下是否存在 TargetTableName == 'bomfilecontent'）
        /// </summary>
        List<EsFileTransferMappingDTO> GetListWithBomFlag(SearchVO searchVO);
    }
}
