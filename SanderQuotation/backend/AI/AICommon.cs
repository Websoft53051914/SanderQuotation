using backend.Common;
using Business.BusinessLogic;
using Business.DomainModel;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using static Const.Enums;

namespace backend.AI
{
    public class AICommon
    {
        public static string GetChatHistoryCacheKey(string sessionId)
        {
            return sessionId;
        }
        public static void LogError(Exception ex, object para, [CallerMemberName] string method = "")
        {
            LogBL logBL = BLFactory.GetInstanceBackGround<LogBL>();
            logBL.InsertLog(new ControlLogDM
            {
                ControllerName = method,
                ActionName = method,
                IP = "127.0.0.1",
                Account = "System",
                Name = "System",
                Status = ((int)LogStatusEnum.Failed),
                Exception = ex.ToString() + " 參數: " + JsonConvert.SerializeObject(para)
            });
        }
    }
}
