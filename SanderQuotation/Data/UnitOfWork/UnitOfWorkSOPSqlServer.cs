using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Enums;
using Data.Register;
using Microsoft.Extensions.Configuration;

namespace Data.UnitOfWork
{
    /// <summary>
    /// SOP 資料庫 UnitOfWork（SQL Server），讀取 appsettings.json 中 ConnectionStrings:SOPConnection
    /// </summary>
    public class UnitOfWorkSOPSqlServer : IUnitOfWorkSOP
    {
        private readonly IDBHelper _dbHelper;

        /// <summary>
        /// 建構時讀取 SOPConnection 並建立 DBHelper
        /// </summary>
        public UnitOfWorkSOPSqlServer()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();
            string connectionString = config["ConnectionStrings:SOPConnection"];
            _dbHelper = new DBHelper_Guid(connectionString, DBTypeEnums.SQL_SERVER);
        }

        /// <summary>
        /// 取得指定 DAO 實例，並注入 SOP DB 連線
        /// </summary>
        public T Repository<T>() where T : IBaseSuperDao
        {
            T dao = DaoFactory.GetDAOInstance<T>();
            dao.DbHelper = _dbHelper;
            return dao;
        }
    }
}
