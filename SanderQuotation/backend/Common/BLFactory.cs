using Business;
using Business.Common;

namespace backend.Common
{
    public class BLFactory
    {
        /// <summary>
        /// 使用此方法取得BL，會自動帶入Session資訊
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetInstance<T>() where T : BaseProjectBL
        {
            T bl = BusinessFactory.GetInstance<T>();
            //bl.SessionVO = LoginSession.Current;
            //bl.I18NRequest = ResourceHelper.GetCultureInfo().Name;
            return bl;
        }

        public static T GetInstance<T>(IConfiguration configuration) where T : BaseProjectBL
        {
            T bl = BusinessFactory.GetInstance<T>();
            //bl.SessionVO = LoginSession.Current;
            bl._Configuration = configuration;
            return bl;
        }

        public static T GetInstanceBackGround<T>(IConfiguration configuration = null) where T : BaseProjectBL
        {
            T bl = BusinessFactory.GetInstance<T>();
            //bl.SessionVO = new SessionVO();
            //bl.SessionVO.AccountId = Guid.Empty;
            bl._Configuration = configuration;
            return bl;
        }

    }
}
