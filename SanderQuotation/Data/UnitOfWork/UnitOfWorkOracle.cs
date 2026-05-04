using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Data.Register;
using Microsoft.Extensions.Configuration;

namespace Data.UnitOfWork
{
    public class UnitOfWorkOracle : IUnitOfWork
    {

        protected IDBHelper? dbHelper = null;
        public UnitOfWorkOracle()
        {
            try
            {
                IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, false).Build();
                var connectionString = config["ConnectionStrings:MainConnection"];
                this.dbHelper = new DBHelper_MySQL((connectionString));
            }
            catch (Exception)
            {
                throw;
            }
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
