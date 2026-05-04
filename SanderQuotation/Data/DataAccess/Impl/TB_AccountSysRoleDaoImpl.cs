using Core.Utility.Base.Data;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{
    public class TB_AccountSysRoleDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_AccountSysRoleEntity>, ITB_AccountSysRoleDAO
    {
        public void DeleteByMemberId(Guid id)
        {
            Dictionary<string, object> paras = new();
            paras.Add("@id", id);

            string strWhere = "";
            //if (oriCompanyId != 0)
            //{
            //    strWhere = " and RoleID in (select id from SysRole where CompanyId=@oriCompanyId) ";
            //}

            string sql = $@"
delete from TB_AccountSysRole
where AccountID=@id
{strWhere}

";

            base.DbHelper.Execute(sql, paras);
        }

        public List<TB_AccountSysRoleEntity> FindListByMemberId(Guid id)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);

            string qrySQL = @"
select * from
TB_AccountSysRole
where AccountId=@id

";

            return DbHelper.FindList<TB_AccountSysRoleEntity>(qrySQL, paras);
        }

        public List<AccountDTO> FindListByMemberIds(List<Guid> ids)
        {
            Dictionary<string, object> paras = new();
            paras.Add("ids", ids);

            string qrySQL = @"
select 
b.id as roleid,
b.RoleName,
a.AccountId as id
from
TB_AccountSysRole a
join TB_sysrole b on a.roleid=b.id

where AccountId in @ids

";

            return DbHelper.FindList<AccountDTO>(qrySQL, paras);
        }
    }
}
