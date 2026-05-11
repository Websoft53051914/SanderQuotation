using CommonClass.Model;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Helper.DB.FilterCondition;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using System.Text;


namespace Data.DataAccess.Impl
{
    public class TB_ControlLogDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_ControlLogEntity>, ITB_ControlLogDAO
    {
        public PageResult<TB_ControlLogEntity> GetPageList(PageEntity pageEntity, int? status, string keyword, DateTime? dateGte, DateTime? dateLte)
        {
            StringBuilder condition = new();
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("Name", "%" + keyword + "%");
            properties.Add("Account", "%" + keyword + "%");
            properties.Add("status", status);
            properties.Add("dateGte", dateGte);
            properties.Add("dateLte", dateLte);

            if (dateGte != null)
            {
                condition.Append(" and LogTime>=@dateGte ");
            }
            if (dateLte != null)
            {
                condition.Append(" and LogTime<=@dateLte ");
            }

            if (status != null)
            {
                condition.Append(" and status=@status ");
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                condition.Append(" and ( Name like @Name or Account like @Account ) ");
            }

            string sql = $@"SELECT 
CL.{nameof(TB_ControlLogEntity.Action)},
CL.{nameof(TB_ControlLogEntity.DataId)},
CL.{nameof(TB_ControlLogEntity.Id)},
    CL.{nameof(TB_ControlLogEntity.LogTime)},
    CL.{nameof(TB_ControlLogEntity.Status)},
    CL.{nameof(TB_ControlLogEntity.Account)},
    CL.{nameof(TB_ControlLogEntity.ControllerName)},
    CL.{nameof(TB_ControlLogEntity.ActionName)},
    CL.{nameof(TB_ControlLogEntity.Name)} 
FROM TB_ControlLog CL WHERE 1=1 {condition}";
            string countSQL = @"
  SELECT  
    count(0)
  FROM 
  (
" + sql + @"
) as pageData
 where 1=1 
";
            return DbHelper.FindPageList<TB_ControlLogEntity>(sql, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, properties, $" {pageEntity.Sort} {pageEntity.Asc}, Id");
        }


        public void  DeleteOldLog(int days)
        {
            var targetDate = DateTime.Now.AddDays(-days);
            Dictionary<string, object> param = new Dictionary<string, object>();
            param.Add("targetDate", targetDate);

            string sql = $@"
DELETE FROM TB_ControlLog
WHERE LogTime < @targetDate;";


            DbHelper.Execute(sql, param);
            DbHelper.Commit();
        }
    }
}
