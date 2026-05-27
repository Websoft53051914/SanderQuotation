using CommonClass.Model;
using Const;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    public class EsScheduleCycleDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<EsScheduleCycleEntity>, IEsScheduleCycleDAO
    {
        public PageResult<EsScheduleCycleEntity> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            var paras = new Dictionary<string, object>();
            string whereSql = $" AND {nameof(EsScheduleCycleEntity.Status)} != @StatusNeq";
            paras.Add("@StatusNeq", StatusEnum.Cancel.ToInt());

            if (!string.IsNullOrWhiteSpace(searchVO.KeywordLike))
            {
                paras.Add("@Keyword1", $"%{searchVO.KeywordLike}%");
                whereSql += $@" AND (
    {nameof(EsScheduleCycleEntity.CycleName)} LIKE @Keyword1 OR
    {nameof(EsScheduleCycleEntity.ScheduleCycleCode)} LIKE @Keyword1
)";
            }

            string sql = $@"SELECT * FROM esScheduleCycle WHERE 1=1 {whereSql}";

            string countSql = @"
SELECT count(0)
FROM (
" + sql + @"
) AS pageData WHERE 1=1";

            string orderBy = $"{pageEntity.Sort} {pageEntity.Asc}, Id";

            return DbHelper.FindPageList<EsScheduleCycleEntity>(sql, countSql, pageEntity.CurrentPage, pageEntity.PageDataSize, paras, orderBy);
        }

        public List<EsScheduleCycleEntity> GetListByFilter(SearchVO searchVO)
        {
            var paras = new Dictionary<string, object>();
            string whereSql = $" AND {nameof(EsScheduleCycleEntity.Status)} != @StatusNeq";
            paras.Add("@StatusNeq", StatusEnum.Cancel.ToInt());
            if (!string.IsNullOrEmpty(searchVO.ScheduleCycleCodeEq))
            {
                whereSql += $" AND {nameof(EsScheduleCycleEntity.ScheduleCycleCode)} = @ScheduleCycleCodeEq";
                paras.Add("@ScheduleCycleCodeEq", searchVO.ScheduleCycleCodeEq);
            }
            string sql = $@"SELECT * FROM esScheduleCycle WHERE 1=1 {whereSql}";
            return DbHelper.FindList<EsScheduleCycleEntity>(sql, paras);
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
                $"SELECT * FROM esScheduleCycleWeekDay WHERE {nameof(EsScheduleCycleWeekDayEntity.ScheduleCycleCode)} IN ({inParams}) AND {nameof(EsScheduleCycleWeekDayEntity.Status)} = {StatusEnum.Enabled.ToInt()}", paras);
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
                $"SELECT * FROM esScheduleCycleMonthDay WHERE {nameof(EsScheduleCycleMonthDayEntity.ScheduleCycleCode)} IN ({inParams}) AND {nameof(EsScheduleCycleMonthDayEntity.Status)} = {StatusEnum.Enabled.ToInt()}", paras);
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
                $"SELECT * FROM esScheduleCycleDbTransfer WHERE {nameof(EsScheduleCycleDbTransferEntity.ScheduleCycleCode)} IN ({inParams}) AND {nameof(EsScheduleCycleDbTransferEntity.Status)} = {StatusEnum.Enabled.ToInt()}", paras);
        }

        public List<EsScheduleCycleDbTransferEntity> GetListByFilter(SearchVO searchVO)
        {
            var paras = new Dictionary<string, object>();
            string whereSql = $" AND {nameof(EsScheduleCycleDbTransferEntity.Status)} != @StatusNeq";
            paras.Add("@StatusNeq", StatusEnum.Cancel.ToInt());
            if (searchVO.TransferCodeIn.Count > 0)
            {
                var inParams = InParamHelper.BuildInParams(searchVO.TransferCodeIn, out var inParas);
                foreach (var kvp in inParas)
                {
                    paras.Add(kvp.Key, kvp.Value);
                }
                whereSql += $" AND {nameof(EsScheduleCycleDbTransferEntity.TransferCode)} IN ({inParams})";
            }
            string sql = $@"SELECT * FROM esScheduleCycleDbTransfer WHERE 1=1 {whereSql}";
            return DbHelper.FindList<EsScheduleCycleDbTransferEntity>(sql, paras);
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
                $"SELECT * FROM esScheduleCycleFileTransfer WHERE {nameof(EsScheduleCycleFileTransferEntity.ScheduleCycleCode)} IN ({inParams}) AND {nameof(EsScheduleCycleFileTransferEntity.Status)} = {StatusEnum.Enabled.ToInt()}", paras);
        }

        public List<EsScheduleCycleFileTransferEntity> GetListByFilter(SearchVO searchVO)
        {
            var paras = new Dictionary<string, object>();
            string whereSql = $" AND {nameof(EsScheduleCycleFileTransferEntity.Status)} != @StatusNeq";
            paras.Add("@StatusNeq", StatusEnum.Cancel.ToInt());
            if (searchVO.TransferCodeIn.Count>0)
            {
                var inParams = InParamHelper.BuildInParams(searchVO.TransferCodeIn, out var inParas);
                foreach (var kvp in inParas)
                {
                    paras.Add(kvp.Key, kvp.Value);
                }
                whereSql += $" AND {nameof(EsScheduleCycleFileTransferEntity.TransferCode)} IN ({inParams})";
            }
            string sql = $@"SELECT * FROM esScheduleCycleFileTransfer WHERE 1=1 {whereSql}";
            return DbHelper.FindList<EsScheduleCycleFileTransferEntity>(sql, paras);
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
