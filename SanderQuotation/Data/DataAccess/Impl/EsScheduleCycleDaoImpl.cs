using CommonClass.Model;
using Core.Utility.Base.Data;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using DocumentFormat.OpenXml.Bibliography;

namespace Data.DataAccess.Impl
{
    public class EsScheduleCycleDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleEntity>, IEsScheduleCycleDAO
    {
        public PageResult<EsScheduleCycleDTO> GetPageList(PageEntity pageEntity, EsScheduleCycleDTO dto)
        {
            var paras = new Dictionary<string, object>();
            string whereSql = "";
             
            string sql = $@"";
             
            string originSQL = $@"

SELECT * FROM esScheduleCycle WHERE 1=1 
{whereSql}

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


            return DbHelper.FindPageList<EsScheduleCycleDTO>(qrySQL, countSQL, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, " id ");
        } 
    }

    public class EsScheduleCycleWeekDayDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleWeekDayEntity>, IEsScheduleCycleWeekDayDAO
    {
        public List<EsScheduleCycleWeekDayEntity> GetByCodes(IEnumerable<string> codes)
        {
            var codeList = codes.ToList();
            if (codeList.Count == 0) return new List<EsScheduleCycleWeekDayEntity>();
            var inParams = InParamHelper.BuildInParams(codeList, out var paras);
            return DbHelper.FindList<EsScheduleCycleWeekDayEntity>(
                $"SELECT * FROM esScheduleCycleWeekDay WHERE {nameof(EsScheduleCycleWeekDayEntity.ScheduleCycleCode)} IN ({inParams})", paras);
        }
    }

    public class EsScheduleCycleMonthDayDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleMonthDayEntity>, IEsScheduleCycleMonthDayDAO
    {
        public List<EsScheduleCycleMonthDayEntity> GetByCodes(IEnumerable<string> codes)
        {
            var codeList = codes.ToList();
            if (codeList.Count == 0) return new List<EsScheduleCycleMonthDayEntity>();
            var inParams = InParamHelper.BuildInParams(codeList, out var paras);
            return DbHelper.FindList<EsScheduleCycleMonthDayEntity>(
                $"SELECT * FROM esScheduleCycleMonthDay WHERE {nameof(EsScheduleCycleMonthDayEntity.ScheduleCycleCode)} IN ({inParams})", paras);
        }
    }

    public class EsScheduleCycleDbTransferDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleDbTransferEntity>, IEsScheduleCycleDbTransferDAO
    {
        public List<EsScheduleCycleDbTransferEntity> GetByCodes(IEnumerable<string> codes)
        {
            var codeList = codes.ToList();
            if (codeList.Count == 0) return new List<EsScheduleCycleDbTransferEntity>();
            var inParams = InParamHelper.BuildInParams(codeList, out var paras);
            return DbHelper.FindList<EsScheduleCycleDbTransferEntity>(
                $"SELECT * FROM esScheduleCycleDbTransfer WHERE {nameof(EsScheduleCycleDbTransferEntity.ScheduleCycleCode)} IN ({inParams})", paras);
        }
    }

    public class EsScheduleCycleExcelDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleFileTransferEntity>, IEsScheduleCycleExcelDAO
    {
        public List<EsScheduleCycleFileTransferEntity> GetByCodes(IEnumerable<string> codes)
        {
            var codeList = codes.ToList();
            if (codeList.Count == 0) return new List<EsScheduleCycleFileTransferEntity>();
            var inParams = InParamHelper.BuildInParams(codeList, out var paras);
            return DbHelper.FindList<EsScheduleCycleFileTransferEntity>(
                $"SELECT * FROM esScheduleCycleFileTransfer WHERE {nameof(EsScheduleCycleFileTransferEntity.ScheduleCycleCode)} IN ({inParams})", paras);
        }
    }

    internal static class InParamHelper
    {
        public static string BuildInParams(List<string> values, out Dictionary<string, object> paras)
        {
            paras = new Dictionary<string, object>();
            var paramNames = new List<string>();
            for (int i = 0; i < values.Count; i++)
            {
                var key = $"@p{i}";
                paramNames.Add(key);
                paras[key] = values[i];
            }
            return string.Join(",", paramNames);
        }
    }
}
