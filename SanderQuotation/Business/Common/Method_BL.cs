using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using static Const.Enums;

namespace Business.Common
{
    public class Method_BL
    {
        public static string EncryptionForPWD(string account, string pwd)
        {
            byte[] salt = Encoding.ASCII.GetBytes(account);

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: pwd,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashed;
        }

        public static bool CheckDateTimeLength(string dateTime, out DateTime check)
        {
            check = DateTime.MinValue;

            if (dateTime == null)
                return false;

            if (dateTime.Length == 8)
                return CheckDateTimeLength8(dateTime, out check);

            if (dateTime.Length == 15)
                return CheckDateTimeLength15(dateTime, out check);

            return false;
        }


        private static bool CheckDateTimeLength15(string dateTime, out DateTime check)
        {
            check = DateTime.MinValue;
            if (!string.IsNullOrEmpty(dateTime))
            {
                if (dateTime.Length == 15)
                {
                    var yyyy = dateTime.Substring(0, 4);
                    var MM = dateTime.Substring(4, 2);
                    var dd = dateTime.Substring(6, 2);
                    var HH = dateTime.Substring(9, 2);
                    var mm = dateTime.Substring(11, 2);
                    var ss = dateTime.Substring(13, 2);

                    var checkStr = $@"{yyyy}/{MM}/{dd} {HH}:{mm}:{ss}";

                    var result = DateTime.TryParse(checkStr, out check);

                    if (result == false)
                        return false;
                }
                else
                    return false;
            }

            return true;
        }


        private static bool CheckDateTimeLength8(string dateTime, out DateTime check)
        {
            check = DateTime.MinValue;
            if (!string.IsNullOrEmpty(dateTime))
            {
                if (dateTime.Length == 8)
                {
                    var yyyy = dateTime.Substring(0, 4);
                    var MM = dateTime.Substring(4, 2);
                    var dd = dateTime.Substring(6, 2);

                    var checkStr = $@"{yyyy}/{MM}/{dd} 00:00:00";

                    var result = DateTime.TryParse(checkStr, out check);

                    if (result == false)
                        return false;
                }
                else
                    return false;
            }

            return true;
        }

        public static string ToDateString(string dbDateTimeString)
        {
            return DateTime.TryParseExact(dbDateTimeString, "yyyyMMdd"
                    , null, System.Globalization.DateTimeStyles.None, out DateTime parseDBDateTimeString)
                    ? parseDBDateTimeString.ToString("yyyy/MM/dd")
                    : string.Empty;
        }

        public static string ToDateForeignString(string dbDateTimeString)
        {
            return DateTime.TryParseExact(dbDateTimeString, "yyyyMMdd"
                    , null, System.Globalization.DateTimeStyles.None, out DateTime parseDBDateTimeString)
                    ? parseDBDateTimeString.ToString("dd/MM/yyyy")
                    : string.Empty;
        }

        public static decimal GetServiceHour(string StartTime, string EndTime)
        {
            var start = DateTime.Parse(StartTime);
            var end = DateTime.Parse(EndTime);
            //.00
            return (decimal)Math.Round(Math.Abs((end - start).TotalHours), 1);
        }

        public static int GetServiceMinute(string StartTime, string EndTime)
        {
            var start = DateTime.Parse(StartTime);
            var end = DateTime.Parse(EndTime);
            return (int)Math.Round(Math.Abs((end - start).TotalMinutes), 0);
        }


        /// <summary>
        /// 將民國年日期轉換為西元日期
        /// </summary>
        /// <param name="rocDate"></param>
        /// <param name="inpurFormat"></param>
        /// <returns></returns>
        public static DateTime ConvertROCtoAD(string rocDate, string inpurFormat = "yyy/MM/dd")
        {
            // 定義民國年格式
            var rocCulture = new CultureInfo("zh-TW");
            rocCulture.DateTimeFormat.Calendar = new TaiwanCalendar();

            // 將民國年日期轉換為 DateTime
            DateTime date = DateTime.ParseExact(rocDate, inpurFormat, rocCulture);

            // 返回西元日期
            return date;
        }


        public static bool IsTimeInRange(TimeSpan current, TimeSpan nightStart, TimeSpan morningEnd)
        {
            if (nightStart <= morningEnd)
            {
                // 例：08:00 ~ 18:00（不跨午夜）
                return current >= nightStart && current <= morningEnd;
            }
            else
            {
                // 例：22:00 ~ 06:00（跨午夜）
                return current >= nightStart || current <= morningEnd;
            }
        }

        /// <summary>
        /// 取得提醒時間
        /// </summary>
        /// <param name="start">開始時間</param>
        /// <param name="intervalHours">間隔小時</param>
        /// <param name="IsNowValid">start是否在晚9~早10 (true 直接發訊息)</param>
        /// <returns></returns>
        public static List<DateTime> GetRemindDatetimes(DateTime start, IConfiguration _config, out bool IsNowValid)
        {
            List<DateTime> result = new List<DateTime>();
            DateTime loopStart = start;
            //禁止發送訊息時間 
            //判斷start時間是否有效
            string Morning = _config["LinePushAlert:Morning"];
            string Night = _config["LinePushAlert:Night"];
            TimeSpan nightStart = TimeSpan.Parse(Night);
            TimeSpan morningEnd = TimeSpan.Parse(Morning);

            bool isWithinRange = IsTimeInRange(start.TimeOfDay, nightStart, morningEnd);
            if (isWithinRange)
            {
                IsNowValid = false;
                loopStart = loopStart.AddDays(1);
                loopStart = new DateTime(loopStart.Year, loopStart.Month, loopStart.Day, morningEnd.Hours, morningEnd.Minutes, 0);
                result.Add(loopStart);
            }
            else
            {
                IsNowValid = true;
            }

            bool flag = true;
            DateTime stop = start.AddHours(int.Parse(_config["LinePushAlert:AlertRange_Hours"]));
            while (flag)
            {
                loopStart = loopStart.AddHours(int.Parse(_config["LinePushAlert:Interval_Hours"]));
                if (loopStart >= stop)
                {
                    flag = false;
                }
                else
                {
                    result.Add(loopStart);
                }
            }

            //去除不合法時間
            result = result.Where(x => !IsTimeInRange(x.TimeOfDay, nightStart, morningEnd)).ToList();

            return result;
        }

        /// <summary>
        /// string 安全轉換成 int ，若轉換失敗則回傳預設值<para/>
        /// 範例：<para/>
        /// CommonUtility.ConvertToInt32("123", 0) => 123<para/>
        /// </summary>
        /// <param name="str">轉換值</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns></returns>
        public static int ConvertToInt32(string str, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(str) || !int.TryParse(str, out int result))
            {
                return defaultValue;
            }

            return result;
        }

        /// <summary>
        /// string 安全轉換成 long ，若轉換失敗則回傳預設值<para/>
        /// 範例：<para/>
        /// CommonUtility.ConvertToLong("123", 0) => 123<para/>
        /// </summary>
        /// <param name="str">轉換值</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns></returns>
        public static long ConvertToLong(string str, long defaultValue)
        {
            if (string.IsNullOrWhiteSpace(str) || !long.TryParse(str, out long result))
            {
                return defaultValue;
            }

            return result;
        }

        public static async Task<string> GetErrorDetailAsync(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            var reason = response.ReasonPhrase;
            string content = await response.Content.ReadAsStringAsync();

            return JsonConvert.SerializeObject(new
            {
                StatusCode = statusCode,
                Reason = reason,
                Content = content
            });
        }
    }
}
