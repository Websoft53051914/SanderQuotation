using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface ITB_SysRoleDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TB_SysRoleEntity>
    {
        void Enable(Guid id, int enable);
        SysRoleDTO FindByName(string roleName);
        List<SysRoleDTO> FindList();
        PageResult<SysRoleDTO> FindPageList(PageEntity pageEntity, SysRoleDTO dto);
        List<PermissionDTO> GetAllFuncList(Guid id);
        SysRoleDTO GetInfo(Guid id);
        bool IsExist(Guid memberId, Guid roleId);
    }
}
