using Core.Utility.Base.Data;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class TB_SysRoleDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_SysRoleEntity>, ITB_SysRoleDAO
    {
        public void Enable(Guid id , int enable)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);
            paras.Add("enable", enable);

            string sql = @"
update TB_SysRole
set status=@enable
where id=@id
";

            base.DbHelper.Execute(sql, paras);
            DbHelper.Commit();
        }

        public SysRoleDTO FindByName(string roleName)
        {
            Dictionary<string, object> paras = new();
            paras.Add("name", roleName);

            string qrySQL = @"

select *
from TB_SysRole
where 1=1
and status!=9
and rolename=@name

";

            return DbHelper.Find<SysRoleDTO>(qrySQL, paras);
        }

        public List<SysRoleDTO> FindList()
        {
            Dictionary<string, object> paras = new();
            string sqlWhere = "";


            string qrySQL = $@"
SELECT a.*
  FROM TB_SysRole a
  where 1=1
and a.status={StatusEnum.Enabled.ToInt()}
{sqlWhere}
order by a.rolename
";

            return DbHelper.FindList<SysRoleDTO>(qrySQL, paras);
        }

        public List<PermissionDTO> GetAllFuncList(Guid id)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);

            string sqlWhere = "";


            string qrySQL = $@"


select 
TBSFD.Id as FuncDetailId ,
TBSFC.ClassName,
TBSF.Name as FuncName,
TBSFD.Sequence,
case when TBSR.id is null then 0 else 1 end as IsPermission

from TB_SysFunc TBSF
left outer join TB_SysFuncDetail TBSFD on TBSF.Id=TBSFD.FuncId
left outer join TB_SysRoleFuncDetail TBSRFD on TBSRFD.FuncDetailID=TBSFD.Id and TBSRFD.RoleID=@id
left outer join TB_SysRole TBSR on TBSR.Id=TBSRFD.RoleID and TBSR.id=@id
left outer join TB_SysFuncClass TBSFC on TBSFC.Id=TBSF.FuncClassId


where 
1=1
and TBSF.Status={StatusEnum.Enabled.ToInt()}
and TBSFD.Status={StatusEnum.Enabled.ToInt()}
{sqlWhere}

order by  TBSF.FuncClassId ,TBSF.Sequence,TBSFD.Sequence


";

            return DbHelper.FindList<PermissionDTO>(qrySQL, paras);
        }

        public PageResult<SysRoleDTO> FindPageList(PageEntity pageEntity, SysRoleDTO dto)
        {
            Dictionary<string, object> paras = new Dictionary<string, object>();

            paras.Add("Memo", "%" + dto.Memo + "%");
            paras.Add("RoleName", "%" + dto.RoleName + "%");
            paras.Add("Status", "%" + dto.Status + "%");

            string whereSQL = "";

            if (!string.IsNullOrEmpty(dto.Memo))
                whereSQL += @" and a.Memo LIKE @Memo  ";

            if (!string.IsNullOrEmpty(dto.RoleName))
                whereSQL += @" and RoleName LIKE @RoleName  ";

            if (dto.Status!=null)
                whereSQL += @" and Status LIKE @Status ";



            string originSQL = $@"

select 
a.*
from 
TB_SysRole a 

where 1=1
and status!=9

{whereSQL}

";

            string qrySQL = @"

  SELECT  pageData.*
  FROM 
  (
" + originSQL + @"
) as pageData  
 where 1=1 

";

            string countSQL = @"
  SELECT  
    count(0)
  FROM 
  (
" + originSQL + @"
) as pageData
 where 1=1 
";

            return DbHelper.FindPageList<SysRoleDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, " id ");
        }

        public SysRoleDTO GetInfo(Guid id)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);

            string qrySQL = @"

select a.*
from TB_SysRole a
where 1=1
and a.id=@id

";

            return DbHelper.Find<SysRoleDTO>(qrySQL, paras);
        }

        public bool IsExist(Guid accountID, Guid roleId)
        {
            Dictionary<string, object> paras = new();
            paras.Add("accountID", accountID);
            paras.Add("RoleID", roleId);

            string qrySQL = @"

select * from
TB_AccountSysRole
where AccountID=@accountID
and RoleID=@RoleID

";

            var entity = DbHelper.Find<SysRoleDTO>(qrySQL, paras);
            if (entity == null)
                return false;

            return true;
        }

    }
}
