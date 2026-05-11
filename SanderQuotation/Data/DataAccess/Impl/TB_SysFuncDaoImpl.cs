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


    public class TB_SysFuncDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_SysFuncEntity>, ITB_SysFuncDAO
    {

        public List<SysFuncDTO> FindListMenu(string memberAccount)
        {
            Dictionary<string, object> paras = new();
            paras.Add("memberAccount", memberAccount);

            string qrySQL = @"

select 
sysfunc.id,
permissioncode,
classname,
SysFunc.name as SysFuncName,
SysFuncDetail.NAME as DetailName,
URL
from 
TB_ACCOUNT member
left outer join TB_AccountSysRole memberrole on member.ID=memberrole.AccountID
left outer join TB_SysRoleFuncDetail SysRoleFuncDetail on SysRoleFuncDetail.roleid=memberrole.RoleID
left outer join TB_SysFuncDetail SysFuncDetail on SysFuncDetail.id=SysRoleFuncDetail.FuncDetailid
left outer join TB_SysFunc SysFunc on SysFuncDetail.funcid=SysFunc.id
left outer join TB_SysFuncCLASS SysFuncCLASS on SysFuncCLASS.id=SysFunc.FUNCCLASSID
left outer join TB_SysRole BackSysRole on BackSysRole.Id=memberrole.RoleID

where member.memberAccount=@memberAccount
and SysFunc.Status='1'
 
 group by 

sysfunc.id,
 permissioncode,
classname,
SysFunc.name,
SysFuncDetail.NAME,
URL,
SysFuncCLASS.SEQUENCE,
SysFunc.SEQUENCE

 order by SysFuncCLASS.SEQUENCE, SysFunc.SEQUENCE,SysFunc.name


";

            return DbHelper.FindList<SysFuncDTO>(qrySQL, paras);
        }

        public List<int> FindPermissionCodesByAccount(string memberAccount)
        {
            Dictionary<string, object> paras = new();
            paras.Add("memberAccount", memberAccount);

            string qrySQL = @"
select SysFuncDetail.PermissionCode
from TB_ACCOUNT member
left outer join TB_AccountSysRole memberrole on member.ID = memberrole.AccountID
left outer join TB_SysRoleFuncDetail SysRoleFuncDetail on SysRoleFuncDetail.roleid = memberrole.RoleID
left outer join TB_SysFuncDetail SysFuncDetail on SysFuncDetail.id = SysRoleFuncDetail.FuncDetailid
left outer join TB_SysFunc SysFunc on SysFuncDetail.funcid = SysFunc.id
where member.memberAccount = @memberAccount
and SysFuncDetail.Status = '1'
and SysFunc.Status = '1'
and SysFuncDetail.PermissionCode is not null
group by SysFuncDetail.PermissionCode
";

            var rawList = DbHelper.FindList<SysFuncDTO>(qrySQL, paras);
            return rawList
                .Where(r => int.TryParse(r.PermissionCode, out _))
                .Select(r => int.Parse(r.PermissionCode))
                .ToList();
        }

        public void Enable(Guid id, int enable)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);
            paras.Add("enable", enable);

            string sql = @"
update TB_SysFunc
set status=@enable
where id=@id
";

            base.DbHelper.Execute(sql, paras);
            DbHelper.Commit();
        }

        public SysFuncDTO FindByName(string name)
        {
            Dictionary<string, object> paras = new();
            paras.Add("name", name);

            string qrySQL = @"

select *
from TB_SysFunc
where 1=1
and status!=9
and name=@name

";

            return DbHelper.Find<SysFuncDTO>(qrySQL, paras);
        }

        public List<SysFuncDTO> FindList(string memberAccount)
        {
            Dictionary<string, object> paras = new();
            paras.Add("memberAccount", memberAccount);

            string qrySQL = @"

select 
SysFunc.ID,
permissioncode,
classname,
SysFunc.name as SysFuncName,
SysFuncDetail.NAME as DetailName,
URL
from 
TB_ACCOUNT member
left outer join TB_AccountSysRole memberrole on member.ID=memberrole.AccountID
left outer join TB_SysRoleFuncDetail SysRoleFuncDetail on SysRoleFuncDetail.roleid=memberrole.RoleID
left outer join TB_SysFuncDetail SysFuncDetail on SysFuncDetail.id=SysRoleFuncDetail.FuncDetailid
left outer join TB_SysFunc SysFunc on SysFuncDetail.funcid=SysFunc.id
left outer join TB_SysFuncCLASS SysFuncCLASS on SysFuncCLASS.id=SysFunc.FUNCCLASSID
left outer join TB_SysRole BackSysRole on BackSysRole.Id=memberrole.RoleID

where member.memberAccount=@memberAccount
and SysFuncDetail.Status='1'
and SysFunc.Status='1'
 
 group by 

 permissioncode,
classname,
SysFunc.name,
SysFuncDetail.NAME,
URL,
SysFuncCLASS.SEQUENCE,
SysFunc.SEQUENCE

 order by SysFuncCLASS.SEQUENCE, SysFunc.SEQUENCE,SysFunc.name,SysFunc.Id


";

            return DbHelper.FindList<SysFuncDTO>(qrySQL, paras);
        }

        public PageResult<SysFuncDTO> FindPageList(PageEntity pageEntity, SysFuncDTO dto)
        {
            Dictionary<string, object> paras = new Dictionary<string, object>();

            paras.Add("Memo", "%" + dto.Memo + "%");
            paras.Add("NAME", "%" + dto.Name + "%");

            string whereSQL = "";

            if (!string.IsNullOrEmpty(dto.Memo))
                whereSQL += @" and a.Memo LIKE @Memo  ";

            if (!string.IsNullOrEmpty(dto.Name))
                whereSQL += @" and NAME LIKE @NAME  ";

            if (dto.FilterFuncClassId.HasValue)
            {
                paras.Add("FuncClassId", dto.FilterFuncClassId.Value);
                whereSQL += @" and a.FuncClassId = @FuncClassId ";
            }

            if (dto.FilterStatus.HasValue)
            {
                paras.Add("FilterStatus", dto.FilterStatus.Value);
                whereSQL += @" and a.Status = @FilterStatus ";
            }

            string originSQL = $@"

select 
a.*,
b.ClassName as  ClassName
from 
TB_SysFunc a left outer join TB_SysFuncClass b on a.FuncClassId=b.Id

where 1=1
and a.status!=9
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

            return DbHelper.FindPageList<SysFuncDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, " Sequence,id ");
        }

        public SysFuncDTO GetInfoByFuncId(string funcId)
        {
            Dictionary<string, object> paras = new();
            paras.Add("funcId", funcId);

            string qrySQL = @"
SELECT tsf.*,tsfc.ClassName FROM TB_SysFunc tsf
left outer join TB_SysFuncClass tsfc on tsf.funcclassid=tsfc.id 
where tsf.id=@funcId

";

            return DbHelper.Find<SysFuncDTO>(qrySQL, paras);
        }

        public List<SysFuncDTO> GetList()
        {
            Dictionary<string, object> paras = new();

            string qrySQL = @"
select * from TB_SysFunc 
where status=1
order by Sequence,FuncClassId

";

            return DbHelper.FindList<SysFuncDTO>(qrySQL, paras);
        }

        public List<SysFuncDTO> GetClassNameList()
        {
            string sql = @$"
SELECT SF.*,
SFC.{nameof(TB_SysFuncClassEntity.ClassName)} AS {nameof(SysFuncDTO.ClassName)} 
FROM TB_SysFunc SF
INNER JOIN TB_SysFuncClass SFC ON SFC.{nameof(TB_SysFuncClassEntity.Id)} = SF.{nameof(TB_SysFuncEntity.FuncClassId)} 
";
            return DbHelper.FindList<SysFuncDTO>(sql);
        }


        public SysFuncDTO GetInfo(string url)
        {
            string sql = @$"
SELECT SF.*,SFC.{nameof(TB_SysFuncClassEntity.ClassName)} AS {nameof(SysFuncDTO.ClassName)} FROM TB_SysFunc SF
INNER JOIN TB_SysFuncClass SFC ON SFC.{nameof(TB_SysFuncClassEntity.Id)} = SF.{nameof(TB_SysFuncEntity.FuncClassId)} 
WHERE SF.{nameof(TB_SysFuncEntity.Url)}= @URL
";
            Dictionary<string, object> paras = new();
            paras.Add("URL", url);

            return DbHelper.Find<SysFuncDTO>(sql, paras);
        }

        public SysFuncDTO GetByFunIdEnum(FuncID funcID)
        {
            Dictionary<string, object> paras = new();
            paras.Add("funcID", funcID.ToInt());
            string SQL = $@"
SELECT 
tsf.Id ,
tsf.Url,
tsc.ClassName as {nameof(SysFuncDTO.ClassName)}
FROM tb_sysfuncdetail tsd
INNER JOIN tb_sysfunc tsf on tsd.FuncId  = tsf.Id 
INNER JOIN tb_sysfuncclass tsc on tsf.FuncClassId = tsc.Id 
WHERE  tsd.PermissionCode = @funcID
";
            return DbHelper.Find<SysFuncDTO>(SQL, paras);
        }
    }

}
