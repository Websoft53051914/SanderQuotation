using Core.Utility.Base.Data;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;


namespace Data.DataAccess.Impl
{

    public class TB_SysFuncDetailDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_SysFuncDetailEntity>, ITB_SysFuncDetailDAO
    {
        public List<SysFuncDetailDTO> FindList()
        {
            Dictionary<string, object> paras = new();

            string qrySQL = @$"

select * from 
TB_SysFuncDetail
where status=1
order by PermissionCode,Sequence

";

            return DbHelper.FindList<SysFuncDetailDTO>(qrySQL, paras);
        }

        public List<SysFuncDetailDTO> FindListByFuncId(Guid id, int? status = null)
        {
            Dictionary<string, object> paras = new();
            paras.Add("id", id);

            string strWhere = "";
            if (status != null)
                strWhere = @$" and a.status={status} ";

            string qrySQL = @$"

select a.*,b.Name as FuncName
from TB_SysFunc b 
left outer join TB_SysFuncDetail a on a.FuncId=b.id  {strWhere}
where 1=1
and b.id=@id
order by Sequence

";
            return DbHelper.FindList<SysFuncDetailDTO>(qrySQL, paras);
        }
    }
}
