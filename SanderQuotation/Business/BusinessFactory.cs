using Data.Register;
using Unity;
using Unity.Resolution;

namespace Business
{
    public class BusinessFactory
    {
        private static IUnityContainer? _Container = null;

        public static T GetInstance<T>()
        {
            return _Container.Resolve<T>();
        }

        public static T GetInstance<T>(ResolverOverride[] param)
        {
            return _Container.Resolve<T>(param);
        }

        public static void Register(IUnityContainer container)
        {
            _Container = container;

            //container
            //    .RegisterType<AccountBL, AccountBL>()


            //    ;

            DaoFactory.Register(container);

            //WMS_WebSite.Data.SQLServer.Register.DaoFactory.Register(container);
        }
    }
}
