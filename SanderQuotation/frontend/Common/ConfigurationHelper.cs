using Const;
using System.Globalization;

namespace frontend.Common.ConfigurationHelper
{
    public class ConfigurationHelper
    {
        public ConfigurationHelper(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        private readonly IConfiguration _configuration;

        public IConfiguration Config => _configuration;

        public string GetMessage(string key, string defaultVal = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return defaultVal;
            }

            return _configuration[$"Message:zh-tw:{key}"] ?? defaultVal;
        }

        /// <summary>
        /// 請輸入 XXX
        /// </summary>
        /// <param name="fieldKey"></param>
        /// <returns></returns>
        public string GetPleaseEnterMessage(string fieldKey)
        {
            string msg = GetMessage("Please_Enter", "Please enter ") + GetMessage(fieldKey, fieldKey);
            return msg;
        }

        public string GetIsExistMessage(string fieldKey)
        {
            string msg = GetMessage(fieldKey, fieldKey) + GetMessage("IsExist", " is exist");
            return msg;
        }




        public string GetBackendUrl()
        {
            var url = _configuration.GetValue<string?>("BackendURL") ?? "";
            return url;
        }

    }
}
