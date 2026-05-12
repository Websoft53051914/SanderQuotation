using Const;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface IESDbTransferDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<ESDbTransferEntity>
    {
        PageResult<ESDbTransferDTO> FindPageList(PageEntity pageEntity, SearchVO searchVO);
    }
}
