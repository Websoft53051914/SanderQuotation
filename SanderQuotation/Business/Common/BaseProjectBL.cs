using Core.Utility.Base.Business;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace Business.Common
{
    public class BaseProjectBL : BaseBL
    {
        public CommonClass.Model.UserInfo? UserInfo = null;

        public DateTime now  = DateTime.Now;

        public IConfiguration? _Configuration { get; set; }

        public SessionVO? SessionVO = null;

        /// <summary>
        /// 依目前 UI 文化讀取 message.json（Message:{culture}:{key}），找不到則嘗試 zh-tw、en-us，最後回傳 fallback。
        /// </summary>
        protected string GetLocalizedMessage(string messageKey, string? fallbackDefault = null)
        {
            if (string.IsNullOrWhiteSpace(messageKey))
            {
                return fallbackDefault ?? string.Empty;
            }

            if (_Configuration == null)
            {
                return fallbackDefault ?? messageKey;
            }

            var culture = CultureInfo.CurrentUICulture.Name;
            var msg = _Configuration[$"Message:{culture}:{messageKey}"];
            if (!string.IsNullOrEmpty(msg))
            {
                return msg;
            }

            msg = _Configuration[$"Message:zh-tw:{messageKey}"];
            if (!string.IsNullOrEmpty(msg))
            {
                return msg;
            }

            msg = _Configuration[$"Message:en-us:{messageKey}"];
            if (!string.IsNullOrEmpty(msg))
            {
                return msg;
            }

            return fallbackDefault ?? messageKey;
        }
    }
}
