using Core.Utility.Base.Data;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;


namespace Data.DataAccess.Impl
{

    public class TB_SysRoleFuncDetailDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_SysRoleFuncDetailEntity>, ITB_SysRoleFuncDetailDAO
    {
        public void DeleteByRoleId(Guid id)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);

            string sql = @"
delete TB_SysRoleFuncDetail
where roleid=@id
";

            base.DbHelper.Execute(sql, paras);
        }

        public List<TB_SysRoleFuncDetailEntity> FindListByRoleId(Guid id)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);

            string qrySQL = @"
select * from
TB_SysRoleFuncDetail
where roleid=@id

";

            return DbHelper.FindList<TB_SysRoleFuncDetailEntity>(qrySQL, paras);
        }


        public List<TB_SysFuncDetailEntity> FindListByMemberAccount(string memberAccount)
        {
            Dictionary<string, object> paras = new();
            paras.Add("memberAccount", memberAccount);
            string sql = @"select  ts.* FROM TB_SysRoleFuncDetail SRFD
			inner join tb_sysfuncdetail ts  on ts.id  = srfd.funcdetailid
            INNER JOIN TB_SysRole SR ON SR.Id = SRFD.RoleId
            inner join tb_accountsysrole tasr on tasr.roleid  = SR.id
            inner join tb_account a on a.id  = tasr.accountid
            where a.memberaccount  = @memberAccount
            ";

            return DbHelper.FindList<TB_SysFuncDetailEntity>(sql, paras);
        }
    }
}
