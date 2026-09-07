using Business.Common;
using Business.DomainModel;
using Core.Utility.Helper.DB;
using Sander.Platform.DbTransfer;

namespace Business.BusinessLogic
{
    /// <summary>
    /// IDbTransferService 實作，轉呼叫套件內 ESDbTransfer*BL。
    /// </summary>
    public class DbTransferService : BaseProjectBL, IDbTransferService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DbTransferService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private ESDbTransferMappingBL CreateMappingBL()
        {
            var bl = new ESDbTransferMappingBL(_unitOfWork, SessionVO);
            bl._Configuration = _Configuration;
            bl.UserInfo = UserInfo;
            return bl;
        }

        private ESDbTransferBL CreateTransferBL()
        {
            var bl = new ESDbTransferBL(_unitOfWork, SessionVO);
            bl._Configuration = _Configuration;
            bl.UserInfo = UserInfo;
            return bl;
        }

        public DbTransferExecuteResult ExecuteTransfer(string transferMappingCode, string secretKey, string secretIV)
        {
            var mappingBL = CreateMappingBL();
            var dm = mappingBL.GetByCode(transferMappingCode);
            if (dm == null)
            {
                return new DbTransferExecuteResult
                {
                    JobStatus = "Failed",
                    ErrorMessage = "找不到指定的傳輸對應設定"
                };
            }

            var log = mappingBL.ExecuteTransfer(
                mappingRowGuid: dm.Id,
                secretKey: secretKey,
                secretIV: secretIV);

            return new DbTransferExecuteResult
            {
                JobStatus = log.JobStatus,
                ErrorMessage = log.ErrorMessage,
                DataCount = log.DataCount,
                ErrorCount = log.ErrorCount,
                ErrorLogs = log.ErrorLogs?.Select(e => new DbTransferErrorLog
                {
                    Exception = e.Exception,
                    Sql = e.Sql
                }).ToList() ?? new List<DbTransferErrorLog>()
            };
        }

        public DbTransferConnectionConfig? GetDbTransferConfig(string dbTransferCode)
        {
            var dm = CreateTransferBL().GetDbTransferConfig(dbTransferCode);
            if (dm == null)
            {
                return null;
            }

            return new DbTransferConnectionConfig
            {
                TransferCode = dm.TransferCode ?? string.Empty,
                TransferName = dm.TransferName ?? string.Empty,
                DbType = dm.DbType ?? string.Empty,
                DbHost = dm.DbHost ?? string.Empty,
                DbPort = dm.DbPort ?? string.Empty,
                DbName = dm.DbName ?? string.Empty,
                DbUser = dm.DbUser ?? string.Empty,
                DbPassword = dm.DbPassword ?? string.Empty
            };
        }

        public IReadOnlyList<DbTransferOptionItem> GetDbTransferOptions()
        {
            return CreateMappingBL().GetDbTransferOptions();
        }
    }
}
