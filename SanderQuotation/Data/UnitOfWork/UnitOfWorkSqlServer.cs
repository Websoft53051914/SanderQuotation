using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Enums;
using Data.Register;
using Microsoft.Extensions.Configuration;

namespace Data.UnitOfWork
{
    public class UnitOfWorkSqlServer : IUnitOfWork
    {
        protected IDBHelper? dbHelper = null;

        public UnitOfWorkSqlServer()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, false).Build();
            var connectionString = config["ConnectionStrings:MainConnection"];
            this.dbHelper = new DBHelper_Guid((connectionString), DBTypeEnums.SQL_SERVER);
        }

        public void Commit()
        {
            this.dbHelper.Commit();
        }

        public T Repository<T>() where T : IBaseSuperDao
        {
            T dao = DaoFactory.GetDAOInstance<T>();
            dao.DbHelper = dbHelper;
            return dao;
        }
    }
}
