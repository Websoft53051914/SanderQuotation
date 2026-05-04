using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Impl
{

    public class TB_SysFuncClassDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TB_SysFuncClassEntity>, ITB_SysFuncClassDAO
    {
        public SysFuncClassDTO FindByName(string name)
        {
            Dictionary<string, object> paras = new();
            paras.Add("name", name);

            string qrySQL = @"

select *
from TB_SysFuncClass
where 1=1
and status!=9
and ClassName=@name

";

            return DbHelper.Find<SysFuncClassDTO>(qrySQL, paras);
        }

        public List<TB_SysFuncClassEntity> FindList()
        {
            Dictionary<string, object> paras = new();

            string qrySQL = @"
SELECT * 
  FROM TB_SysFuncClass
  where 1=1
{0}
";

            string strWhere = "";
            qrySQL = string.Format(qrySQL, strWhere);

            return DbHelper.FindList<TB_SysFuncClassEntity>(qrySQL, paras);
        }

        public PageResult<SysFuncClassDTO> FindPageList(PageEntity pageEntity, SysFuncClassDTO dto)
        {
            Dictionary<string, object> paras = new Dictionary<string, object>();

            paras.Add("Memo", "%" + dto.Memo + "%");
            paras.Add("CLASSNAME", "%" + dto.ClassName + "%");
            paras.Add("Status", "%" + dto.Status + "%");

            string whereSQL = "";

            if (!string.IsNullOrEmpty(dto.Memo))
                whereSQL += @" and Memo LIKE @Memo  ";

            if (!string.IsNullOrEmpty(dto.ClassName))
                whereSQL += @" and CLASSNAME LIKE @CLASSNAME  ";

            if (dto.Status != null)
                whereSQL += @" and Status LIKE @Status ";

            string originSQL = $@"

select 
*
from 
TB_SysFuncClass a
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

            return DbHelper.FindPageList<SysFuncClassDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, " ClassName ");
        }

        public TB_SysFuncClassEntity FindFuncClass(string url)
        {
            string sql = @"
SELECT * FROM TB_SysFuncClass SFC 
INNER JOIN TB_SysFunc SF ON SFC.ID = SF.FUNCCLASSID 
WHERE SF.URL= @URL
";
            Dictionary<string, object> paras = new();
            paras.Add("URL", url);

            return DbHelper.Find<TB_SysFuncClassEntity>(sql, paras);
        }
    }

}
