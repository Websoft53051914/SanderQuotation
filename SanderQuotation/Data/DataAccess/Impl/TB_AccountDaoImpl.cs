using Const;
using Core.Utility.Base.Data;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using Microsoft.Graph.Models.Security;
using System.Text;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class TB_AccountDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_AccountEntity>, ITB_AccountDAO
    {

        public List<AccountDTO> GetBackMembersByRole(Guid roleId)
        {
            string sql = $@"
SELECT tbm.* FROM TB_ACCOUNT tbm
            INNER JOIN TB_ACCOUNTSysRole tbsr ON tbm.ID = tbsr.AccountId AND AccountStatus = '1'
            WHERE tbsr.RoleID = @RoleID AND tbm.Organization_Id = @Organization_Id
";

            Dictionary<string, object> paras = new();
            paras.Add("RoleID", roleId);
            return DbHelper.FindList<AccountDTO>(sql, paras);
        }

        public List<AccountDTO> GetBackMembersByRole(List<Guid> roleIds)
        {
            string sql = $@"
SELECT DISTINCT tbm.* FROM TB_ACCOUNT tbm
            INNER JOIN TB_ACCOUNTSysRole tbsr ON tbm.ID = tbsr.AccountId AND AccountStatus = '1'
			INNER JOIN TB_SysRole SR ON SR.ID = tbsr.ROLEID
            WHERE tbsr.RoleID IN @RoleID AND SR.Organization_Id = @Organization_Id
";

            Dictionary<string, object> paras = new();
            paras.Add("RoleID", roleIds);
            return DbHelper.FindList<AccountDTO>(sql, paras);
        }

        public void Enable(Guid id, int enable)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);
            paras.Add("enable", enable);

            string sql = @"
update TB_ACCOUNT
set AccountStatus=@enable
where id=@id
";

            base.DbHelper.Execute(sql, paras);
            DbHelper.Commit();
        }

        public AccountDTO FindByAccount(string memberAccount)
        {
            Dictionary<string, object> paras = new();
            paras.Add("MemberAccount", memberAccount);
            paras.Add("Status", AccountStatusEnum.Cancel.ToInt().ToString());

            string qrySQL = $@"


select  
a.*
from
TB_ACCOUNT a
where MemberAccount=@MemberAccount and {nameof(TB_AccountEntity.AccountStatus)} != @Status

";

            return DbHelper.Find<AccountDTO>(qrySQL, paras);
        }


        public AccountDTO FindDTOByPk(Guid id)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);

            string qrySQL = @"

select 
a.*

from
TB_ACCOUNT a 

where a.id=@id

";

            return DbHelper.Find<AccountDTO>(qrySQL, paras);
        }
        public PageResult<AccountDTO> FindPageList(PageEntity pageEntity, AccountDTO dto, bool isExpaied)
        {
            Dictionary<string, object> paras = new Dictionary<string, object>();
            paras.Add("AccountName", "%" + dto.AccountName + "%");
            paras.Add("MemberAccount", "%" + dto.MemberAccount + "%");
            paras.Add("RoleName", "%" + dto.RoleName + "%");
            paras.Add("AccountStatus", dto.AccountStatus);
            paras.Add("RoleId", dto.RoleId);

            string whereSQL = "";



            if (!string.IsNullOrEmpty(dto.AccountName))
                whereSQL += @" and AccountName LIKE @AccountName ";

            if (!string.IsNullOrEmpty(dto.MemberAccount))
                whereSQL += @" and MemberAccount LIKE @MemberAccount ";


            if (!string.IsNullOrEmpty(dto.RoleName))
                whereSQL += @" and a.id in (select AccountId from tb_Accountsysrole where roleid in (select id from tb_sysrole where rolename like @RoleName ) ) ";

            if (!string.IsNullOrEmpty(dto.AccountStatus))
            {
                whereSQL += @" and AccountStatus = @AccountStatus ";
            }
            else
            {
                whereSQL += $@" and AccountStatus != '{AccountStatusEnum.Cancel.ToInt()}' ";
            }
             



            if (isExpaied)
                whereSQL += $@" 
 and DATEADD(DAY, 180, (
 SUBSTRING ( LastLoginTime ,1 , 4 )+'-'+
SUBSTRING ( LastLoginTime ,5 , 2 )+'-'+
SUBSTRING ( LastLoginTime ,7 , 2 )+' '+
SUBSTRING ( LastLoginTime ,10 , 2 )+':'+
SUBSTRING ( LastLoginTime ,12 , 2 )+':'+
SUBSTRING ( LastLoginTime ,14 , 2 ))
) <= SYSDATETIME() and accountstatus='{AccountStatusEnum.Enabled.ToInt()}'
";

            string originSQL = $@"

select  
a.* FROM TB_account a

where 1=1

and a.AccountStatus!='{AccountStatusEnum.Cancel.ToInt()}'

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

            return DbHelper.FindPageList<AccountDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, "accountName, AccountStatus, Id");

        }



        public TB_AccountEntity FindByAccountName(string accountName)
        {
            Dictionary<string, object> paras = new();
            paras.Add("accountName", accountName);

            string qrySQL = @"
select * from
TB_Account
where accountName=@accountName

";

            return DbHelper.Find<TB_AccountEntity>(qrySQL, paras);
        }

        public PageResult<AccountDTO> GetUnRolePageList(PageEntity pageEntity, AccountDTO dto, bool isExpaied)
        {
            Dictionary<string, object> paras = new Dictionary<string, object>();
            paras.Add("AccountName", "%" + dto.AccountName + "%");
            paras.Add("MemberAccount", "%" + dto.MemberAccount + "%");
            //paras.Add("RoleName", "%" + dto.RoleName + "%");
            //paras.Add("AccountStatus", dto.AccountStatus);
            paras.Add("RoleId", dto.RoleId);

            string whereSQL = "";

            if (!string.IsNullOrEmpty(dto.AccountName))
                whereSQL += @" and AccountName LIKE @AccountName ";

            if (!string.IsNullOrEmpty(dto.MemberAccount))
                whereSQL += @" and MemberAccount LIKE @MemberAccount ";


            //if (!string.IsNullOrEmpty(dto.RoleName))
            //    whereSQL += @" and a.id in (select memberid from membersysrole where roleid in (select id from sysrole where rolename like @RoleName ) ) ";

            //if (!string.IsNullOrEmpty(dto.AccountStatus))
            //    whereSQL += @" and AccountStatus = @AccountStatus ";

            if (isExpaied)
                whereSQL += @" 
 and DATEADD(DAY, 180, (
 SUBSTRING ( LastLoginTime ,1 , 4 )+'-'+
SUBSTRING ( LastLoginTime ,5 , 2 )+'-'+
SUBSTRING ( LastLoginTime ,7 , 2 )+' '+
SUBSTRING ( LastLoginTime ,10 , 2 )+':'+
SUBSTRING ( LastLoginTime ,12 , 2 )+':'+
SUBSTRING ( LastLoginTime ,14 , 2 ))
) <= SYSDATETIME() and accountstatus=1 
";

            string originSQL = $@"

select a.* from TB_account a

where 1=1

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

            return DbHelper.FindPageList<AccountDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, "accountstatus");
        }


        public List<AccountDTO> GetLast3PWD(string memberAccount)
        {
            Dictionary<string, object> paras = new();
            paras.Add("MemberAccount", memberAccount);

            string qrySQL = @"

select  b.* from
TB_Account a
left outer join
TB_AccountPWDLog b on a.ID=b.ACCOUNTID

where MemberAccount=@memberAccount

order by createtime  limit 3


";

            return DbHelper.FindList<AccountDTO>(qrySQL, paras);
        }
    }
}
