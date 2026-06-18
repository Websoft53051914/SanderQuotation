using Const;
using System.Globalization;

namespace backend.Common.ConfigurationHelper
{
    public class ConfigurationHelper
    {
        public ConfigurationHelper(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        private readonly IConfiguration _configuration;



        public string GetMessage(string key, string defaultVal = "")
        {
            defaultVal = string.IsNullOrEmpty(defaultVal) ? key : defaultVal;
            if (string.IsNullOrWhiteSpace(key))
            {
                return defaultVal;
            }

            return _configuration[$"Message:{CultureInfo.CurrentUICulture.Name}:{key}"] ?? defaultVal;
        }

        public string GetMessageBackground(string key, string defaultVal = "")
        {
            defaultVal = string.IsNullOrEmpty(defaultVal) ? key : defaultVal;
            if (string.IsNullOrWhiteSpace(key))
            {
                return defaultVal;
            }

            return _configuration[$"Message:{LocaleConst.ZH_TW}:{key}"] ?? defaultVal;
        }

        public string GetIPCameraUrl()
        {
            var value = _configuration.GetValue<string?>("IPCameraUrl") ?? "";
            return value;
        }

        private static readonly Dictionary<string, string> _dictDefaultValueConfig = new()
        {
            #region -- 外部查價 --
            // 外部查價效期天數
            { "ExternalQuotation:ExpirationDay", "7" },
            // Nexar API ClientId
            { "ExternalQuotation:Nexar:ClientId", string.Empty },
            // Nexar API ClientSecret
            { "ExternalQuotation:Nexar:ClientSecret", string.Empty },
	        #endregion
            #region -- 內部查價 --
            // 內部查價效期天數
            { "InternalQuotation:ExpirationDay", "1" },
	        #endregion
            #region -- DataCleanupJob --
            // AI 產生檔案保留天數
            { "DataCleanupSettings:AIFileRetentionDays", "1" },
	        #endregion
            #region -- ExtractKeywordJob --
            // 連續失敗批次達此數提前終止；0 表示不中止，持續處理至無 flag=1 或達最長執行時間
            { "ExtractKeyword:AbortAfterConsecutiveFailedBatches", "0" },
            // 整批失敗後暫停秒數（讓 API 恢復，再處理下一批）
            { "ExtractKeyword:FailedBatchCooldownSeconds", "5" },
	        #endregion
            
        };

        /// <summary>
        /// 取得設定值(int)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="defaultVal">預設值</param>
        /// <returns></returns>
        public int GetIntValue(string key, int? defaultVal = null)
        {
            int defaultValConfig = Convert.ToInt32(_dictDefaultValueConfig.GetValueOrDefault(key, "0"));

            return _configuration.GetValue<int?>(key) ?? defaultVal ?? defaultValConfig;
        }

        /// <summary>
        /// 取得設定值(string)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="defaultVal">預設值</param>
        /// <returns></returns>
        public string GetStringValue(string key, string? defaultVal = null)
        {
            string defaultValConfig = _dictDefaultValueConfig.GetValueOrDefault(key, string.Empty);

            return _configuration.GetValue<string?>(key) ?? defaultVal ?? defaultValConfig;
        }

        /// <summary>
        /// 取得公告訊息寄送時間
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetValueSendMessageServiceCommunityNoticeSendTime()
        {
            string key = "SendMessageService:CommunityNoticeSendTime";
            string defaultValConfig = _dictDefaultValueConfig.GetValueOrDefault(key, string.Empty);
            TimeSpan defaultVal = TimeSpan.Parse(defaultValConfig);
            string? value = _configuration.GetValue<string?>(key);

            return !string.IsNullOrEmpty(value) && TimeSpan.TryParse(value, out TimeSpan oVal)
                ? oVal : defaultVal;
        }
    }
}
