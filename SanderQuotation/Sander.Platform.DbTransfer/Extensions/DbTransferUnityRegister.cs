using Business.BusinessLogic;
using Data.DataAccess.Dao;
using Data.DataAccess.Impl;
using Unity;

namespace Sander.Platform.DbTransfer
{
    /// <summary>
    /// Unity 註冊 DbTransfer DAO 與 <see cref="IDbTransferService"/>。由 host 在 <c>DaoFactory</c> 之後呼叫。
    /// </summary>
    public static class DbTransferUnityRegister
    {
        public static IUnityContainer AddDbTransferDao(this IUnityContainer container)
        {
            ArgumentNullException.ThrowIfNull(container);
            container
                .RegisterType<IESDbTransferDAO, ESDbTransferDaoImpl>()
                .RegisterType<IESDbTransferMappingDAO, ESDbTransferMappingDaoImpl>()
                .RegisterType<IESDbTransferMappingColumnDAO, ESDbTransferMappingColumnDaoImpl>()
                .RegisterType<IDbTransferService, DbTransferService>();
            return container;
        }

        /// <summary>同 <see cref="AddDbTransferDao"/>，保留給既有呼叫端。</summary>
        public static void Register(IUnityContainer container) => AddDbTransferDao(container);
    }
}
