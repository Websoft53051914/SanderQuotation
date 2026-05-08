using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Data.Register;
using Microsoft.Extensions.Configuration;
using Unity;

namespace Data.UnitOfWork
{
    public class UnitOfWorkPgSQL : IUnitOfWork
    {
        //private readonly IConfiguration configuration;

        protected IDBHelper? dbHelper = null;
        public UnitOfWorkPgSQL()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, false).Build();
            var connectionString = config["ConnectionStrings:MainConnection"];
            this.dbHelper = new DBHelper_PgSQLG(connectionString);
            //依賴注入動態注入DBHelper
            IUnityContainer? container = DaoFactory.Container();
            container.RegisterFactory<IDBHelper>(c => dbHelper);
        }

        public void Commit()
        {
            this.dbHelper.Commit();
        }

        public T Repository<T>() where T : IBaseSuperDao
        {
            T dao = DaoFactory.GetDAOInstance<T>();
            dao.DbHelper = this.dbHelper;
            return dao;
        }
    }
}
