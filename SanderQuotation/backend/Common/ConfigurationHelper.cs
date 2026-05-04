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
            // 社區公告顯示畫面設定
            // 取得動態資料(公告、廣告、警告、版型更新)的間隔秒數
            { "NoticeTemplate:GetDataIntervalSec", "10" },
            // 預設廣告輪詢秒數
            { "NoticeTemplate:DefaultAdPlaySec", "5" },
            // 公告輪詢秒數
            { "NoticeTemplate:NoticeIntervalSec", "10" },
            // 重整頁面秒數
            { "NoticeTemplate:ReloadSec", "1800" },
            // 公告跑馬燈動畫時間
            { "NoticeTemplate:MarqueeSpeedSec", "15" },
            // 車道顯示畫面設定
            // 取得動態資料(警告、版型更新)的間隔秒數
            { "CarTemplate:GetDataIntervalSec", "10" },
            // 燈號內文字樣式
            { "CarTemplate:SignSecondTextStyle", "font-family:sans-serif;font-size:100px;color:white" },
            // 燈號內秒數樣式
            { "CarTemplate:SignSecondNumberStyle", "font-family:sans-serif;font-size:230px;color:white" },
             // 重整頁面秒數
            { "CarTemplate:ReloadSec", "1800" },
            // Line 推播
            // 公告訊息寄送時間
            { "SendMessageService:CommunityNoticeSendTime", "08:00" },
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
