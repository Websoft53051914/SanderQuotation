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
    }
}
