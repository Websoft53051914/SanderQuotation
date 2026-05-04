using Core.Utility.Base.Data;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface ITB_AccountSysRoleDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TB_AccountSysRoleEntity>
    {
        void DeleteByMemberId(Guid id);
        List<TB_AccountSysRoleEntity> FindListByMemberId(Guid id);
        List<AccountDTO> FindListByMemberIds(List<Guid> list);
    }
}
