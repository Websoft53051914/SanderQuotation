using Business.Common;
using CommonClass.Model;
using Core.Utility.Utility;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using static Const.Enums;
using System.Text.Json;
using Core.Utility.Extensions;

namespace backend.Common
{
    public partial class Method
    {
        public static List<SelectListItem> GetPermissionCode()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "1", Text = "查詢" });
            list.Add(new SelectListItem() { Value = "2", Text = "新增" });
            list.Add(new SelectListItem() { Value = "3", Text = "編輯" });
            list.Add(new SelectListItem() { Value = "4", Text = "作廢" });
            return list;
        }
        public static List<SelectListItem> GetAccountStatusForCreate()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "3", Text = "開通中" });
            return list;
        }

        // --- 安全 API（Checkmarx/ Fortify 能辨識） ---
        internal static string CookieSafeEncode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim();

            // 第一層：清除注入字元（Checkmarx 看得見）
            value = Regex.Replace(value, @"[<>""'`;\\]", "");

            // 第二層：UrlEncode → 防止 cookie injection
            return HttpUtility.UrlEncode(value);
        }

        internal static List<SelectListItem> GetYesNoList()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "Y", Text = "是" });
            list.Add(new SelectListItem() { Value = "N", Text = "否" });
            return list;
        }

        internal static List<SelectListItem> GetBllodTypeList()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "A", Text = "A" });
            list.Add(new SelectListItem() { Value = "B", Text = "B" });
            list.Add(new SelectListItem() { Value = "AB", Text = "AB" });
            list.Add(new SelectListItem() { Value = "O", Text = "O" });
            return list;
        }

        public static string SendMailByGmail(string mailSubject, string mailContent, string userEmail)
        {
            try
            {

                // Google 發信帳號密碼
                string mailUserID = Method.GetAppSettingsDataByName("MailUserID");
                string mailUserPwd = Method.GetAppSettingsDataByName("MailUserPwd");
                string smtpServer = Method.GetAppSettingsDataByName("SmtpServer");
                string smtpPort = Method.GetAppSettingsDataByName("SmtpPort");
                string enableSsl = Method.GetAppSettingsDataByName("EnableSsl");
                int intSmtpPort = int.Parse(smtpPort);
                if (string.IsNullOrEmpty(mailUserID)
                    || string.IsNullOrEmpty(mailUserPwd)
                    || string.IsNullOrEmpty(smtpServer)
                    || string.IsNullOrEmpty(smtpPort)
                    )
                {
                    return "MAIL SERVER相關帳號密碼未設定，請洽詢管理員";
                }

                // 使用 Google Mail Server 發信
                //string SmtpServer = "smtp.gmail.com";
                //int SmtpPort = 587;
                MailMessage mms = new();
                mms.From = new MailAddress(mailUserID);
                mms.Subject = mailSubject;
                mms.Body = mailContent;
                mms.IsBodyHtml = true;
                mms.SubjectEncoding = Encoding.UTF8;
                mms.To.Add(new MailAddress(userEmail));
                using (SmtpClient client = new(smtpServer, intSmtpPort))
                {
                    client.EnableSsl = bool.Parse(enableSsl);
                    client.Credentials = new NetworkCredential(mailUserID, mailUserPwd);//寄信帳密 
                    client.Send(mms); //寄出信件
                }

                return string.Empty;

            }
            catch (Exception ex)
            {
                return "信件發送失敗" + ex.ToString();
            }
        }



        public static List<T> DataTableToList<T>(DataTable dt) where T : class, new()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var objectProperties = typeof(T).GetProperties(flags);
            var columnNames = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var list = dt.AsEnumerable().Select(dataRow =>
            {
                var instanceOfT = Activator.CreateInstance<T>();
                var propertiesList = objectProperties.Where(properties => columnNames.Contains(properties.Name)
                && properties.CanWrite
                && dataRow[properties.Name] != null
                && dataRow[properties.Name] != DBNull.Value);
                foreach (var properties in propertiesList)
                {
                    var type = properties.PropertyType;
                    if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        type = Nullable.GetUnderlyingType(type);
                    }
                    var value = Convert.ChangeType(dataRow[properties.Name], type);
                    //properties.SetValue(instanceOfT, dataRow[properties.Name], null);
                    properties.SetValue(instanceOfT, value, null);
                }
                return instanceOfT;
            }).ToList();
            return list;
        }

        /// <summary>
        /// 設定Session資訊
        /// </summary>
        /// <param name="loginDM"></param>
        /// <param name="roleDMs"></param>
        public static void SetToSession(Business.DomainModel.LoginDM loginDM)
        {
            DateTime dtTime;
            var _Current = LoginSession.Current;
            _Current.AccountId = loginDM.Id;
            _Current.AccountName = loginDM.AccountName;
            _Current.Account = loginDM.MemberAccount;
            _Current.Functions = loginDM.Functions;

            _Current.TempAccount = loginDM.TempAccount;
            _Current.LineSetting = loginDM.LineSetting;

            LoginSession.Current = _Current;
        }

        public static void SetToSession(SessionVO vo)
        {
            LoginSession.Current = vo;

        }


        internal static object GetMonthList()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            for (int i = 1; i <= 12; i++)
            {
                list.Add(new SelectListItem() { Value = i.ToString(), Text = i.ToString() });
            }
            return list;
        }

        /// <summary>
        /// 檢核中華民國外僑及大陸人士在台居留證(舊式+新式)
        /// </summary>
        /// <param name="idNo">身分證</param>
        /// <returns></returns>
        public static bool CheckResidentID(string idNo)
        {
            if (idNo == null)
            {
                return false;
            }
            idNo = idNo.ToUpper();
            Regex regex = new Regex(@"^([A-Z])(A|B|C|D|8|9)(\d{8})$");
            Match match = regex.Match(idNo);
            if (!match.Success)
            {
                return false;
            }

            if ("ABCD".IndexOf(match.Groups[2].Value) >= 0)
            {
                //舊式
                return CheckOldResidentID(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
            }
            else
            {
                //新式(2021/01/02)正式生效
                return CheckNewResidentID(match.Groups[1].Value, match.Groups[2].Value + match.Groups[3].Value);
            }
        }
        /// <summary>
        /// 舊式檢核
        /// </summary>
        /// <param name="firstLetter">第1碼英文字母(區域碼)</param>
        /// <param name="secondLetter">第2碼英文字母(性別碼)</param>
        /// <param name="num">第3~9流水號 + 第10碼檢查碼</param>
        /// <returns></returns>
        private static bool CheckOldResidentID(string firstLetter, string secondLetter, string num)
        {
            //建立字母對應表(A~Z)
            //A=10 B=11 C=12 D=13 E=14 F=15 G=16 H=17 J=18 K=19 L=20 M=21 N=22
            //P=23 Q=24 R=25 S=26 T=27 U=28 V=29 X=30 Y=31 W=32  Z=33 I=34 O=35 
            string alphabet = "ABCDEFGHJKLMNPQRSTUVXYWZIO";
            string transferIdNo =
                $"{alphabet.IndexOf(firstLetter) + 10}" +
                $"{(alphabet.IndexOf(secondLetter) + 10) % 10}" +
                $"{num}";
            int[] idNoArray = transferIdNo.ToCharArray()
                                          .Select(c => Convert.ToInt32(c.ToString()))
                                          .ToArray();

            int sum = idNoArray[0];
            int[] weight = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 1 };
            for (int i = 0; i < weight.Length; i++)
            {
                sum += weight[i] * idNoArray[i + 1];
            }
            return (sum % 10 == 0);
        }
        /// <summary>
        /// 新式檢核
        /// </summary>
        /// <param name="firstLetter">第1碼英文字母(區域碼)</param>
        /// <param name="num">第2碼(性別碼) + 第3~9流水號 + 第10碼檢查碼</param>
        /// <returns></returns>
        private static bool CheckNewResidentID(string firstLetter, string num)
        {
            //建立字母對應表(A~Z)
            //A=10 B=11 C=12 D=13 E=14 F=15 G=16 H=17 J=18 K=19 L=20 M=21 N=22
            //P=23 Q=24 R=25 S=26 T=27 U=28 V=29 X=30 Y=31 W=32  Z=33 I=34 O=35 
            string alphabet = "ABCDEFGHJKLMNPQRSTUVXYWZIO";
            string transferIdNo = $"{(alphabet.IndexOf(firstLetter) + 10)}" +
                                  $"{num}";
            int[] idNoArray = transferIdNo.ToCharArray()
                                          .Select(c => Convert.ToInt32(c.ToString()))
                                          .ToArray();

            int sum = idNoArray[0];
            int[] weight = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 1 };
            for (int i = 0; i < weight.Length; i++)
            {
                sum += (weight[i] * idNoArray[i + 1]) % 10;
            }
            return (sum % 10 == 0);
        }


        /// <summary>
        /// 檢查身分證格式
        /// </summary>
        /// <param name="idnumber"></param>
        /// <returns></returns>
        public static bool IsIdentificationId(string idnumber)
        {
            var result = false;
            if (idnumber.Length == 10)
            {
                idnumber = idnumber.ToUpper();
                if (idnumber[0] >= 0x41 && idnumber[0] <= 0x5A)
                {
                    var a = new[] { 10, 11, 12, 13, 14, 15, 16, 17, 34, 18, 19, 20, 21, 22, 35, 23, 24, 25, 26, 27, 28, 29, 32, 30, 31, 33 };
                    var b = new int[11];
                    b[1] = a[(idnumber[0]) - 65] % 10;
                    var c = b[0] = a[(idnumber[0]) - 65] / 10;
                    for (var i = 1; i <= 9; i++)
                    {
                        b[i + 1] = idnumber[i] - 48;
                        c += b[i] * (10 - i);
                    }
                    if (((c % 10) + b[10]) % 10 == 0)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 檢查統一編號
        /// </summary>
        /// <param name="cTax"></param>
        /// <returns></returns>
        public static (bool IsTrue, string cMessage) CheckTaxID(string cTax)
        { //回傳結果集
            var oResult = (IsTrue: true, cMessage: "統一編號格式合法");
            //邏輯乘數（財政部制定）
            var cMagic = "12121241";
            try
            {
                if (string.IsNullOrEmpty(cTax) || cTax.Length != 8 || !int.TryParse(cTax, out int iUnused))
                { throw new System.Exception("統一編號請輸入八位數純數字"); }
                //轉成數值陣列
                var aryTax = cTax.ToCharArray().Select(x => (int)(x - '0')).ToArray();
                var aryMagic = cMagic.ToCharArray().Select(x => (int)(x - '0')).ToArray();
                //運算乘積
                var aryResult = new int[8];
                for (int i = 0; i < aryTax.Length; i++)
                { aryResult[i] = aryTax[i] * aryMagic[i]; }
                //運算整理：大於10就進行位數相加
                aryResult = aryResult.Select(x => x < 10 ? x : x.ToString().ToCharArray().Select(y => (int)(y - '0')).Sum()).ToArray();
                //運算整理：第七位數大於10之分拆
                var oList = new System.Collections.Generic.List<int[]>();
                foreach (var cItem in aryResult[6].ToString().ToCharArray())
                {
                    var aryTemp = aryResult.ToArray();
                    aryTemp[6] = (int)(cItem - '0');
                    oList.Add(aryTemp);
                }
                //運算整理：乘積和與除5判斷
                if (!oList.Select(x => x.Sum()).Select(x => x % 5 == 0).Any(x => x))
                { throw new System.Exception("格式錯誤"); }
            }
            catch (Exception ex)
            {
                oResult.IsTrue = false;
                oResult.cMessage = ex.ToString();
            }
            return oResult;
        }
        public static string GetClientIPAddress()
        {
            var context = HttpContext.Current;
            //string ClientIP = context.GetServerVariable("HTTP_X_FORWARDED_FOR");
            //if (String.IsNullOrEmpty(ClientIP))
            //{
            //    ClientIP = context.GetServerVariable("REMOTE_ADDR")?.ToString() ?? "::1";
            //}
            //ClientIP = ClientIP.Replace("::1", "127.0.0.1");
            //return ClientIP;
            // Cloudflare
            if (context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp))
                return cfIp.ToString();

            // Nginx / Proxy
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
                return xff.ToString().Split(',')[0].Trim();

            // IIS / Azure
            if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
                return realIp.ToString();

            // Fallback
            var ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? "";
            ClientIP = ClientIP.Replace("::1", "127.0.0.1");
            return ClientIP;
        }


        public static string MixUnicodeToString(string mixUnicode)
        {
            byte[] textBytes = Encoding.Unicode.GetBytes(mixUnicode);
            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.UTF8, textBytes));
        }


        /// <summary>
        /// 判斷是否為正確網址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsValidUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        /// <summary>
        /// 判斷是否為合法的郵件地址
        /// </summary>
        /// <param name="MailAddress">郵件地址</param>
        public static bool IsValidMailAddress(string MailAddress)
        {
            string RegPattern = @"[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}$";
            Regex _tmpRegex = new Regex(RegPattern, RegexOptions.IgnoreCase);
            return _tmpRegex.IsMatch(MailAddress);
        }

        /// <summary>
        /// 判斷是否為合法的台灣手機號碼
        /// </summary>
        /// <param name="CellPhoneNumber">手機號碼</param>
        public static bool IsValidCellPhoneNummberTW(string CellPhoneNumber)
        {
            string RegPattern = @"^(09)([0-9]{2})([-]?)([0-9]{6})$";
            Regex _tmpRegex = new Regex(RegPattern, RegexOptions.IgnoreCase);
            return _tmpRegex.IsMatch(CellPhoneNumber);
        }

        /// <summary>
        /// 判斷是否為合法的台灣市話號碼
        /// </summary>
        /// <param name="PhoneNumber">手機號碼</param>
        public static bool IsValidPhoneNummberTW(string PhoneNumber)
        {
            string RegPattern = @"^(0)([0-9]{1})([-]?)([0-9]{6,8})$";
            Regex _tmpRegex = new Regex(RegPattern, RegexOptions.IgnoreCase);
            return _tmpRegex.IsMatch(PhoneNumber);
        }

        public static string SaveFile(IFormFile file)
        {
            Guid fileId = Guid.NewGuid();
            var lo = System.IO.Path.GetFullPath(System.Configuration.ConfigurationManager.AppSettings["FileLocation"]);

            //if (file.Length > 0)
            {
                var loc = $@"{lo}\{fileId.ToString()}{System.IO.Path.GetExtension(file.FileName)}";
                loc = loc.Replace("..", "");

                using (var stream = System.IO.File.Create(loc))
                {
                    file.CopyTo(stream);
                }

                return loc;
            }

            //return null;
        }




        /// <summary>
        /// 上傳檔案至指定目錄
        /// </summary>
        /// <param name="file">檔案</param>
        /// <param name="DirName">目錄名稱</param>
        /// <param name="_env"></param>
        /// <returns>guid 檔案名稱</returns>
        public static async Task<string> SaveFileAsync(IFormFile file, string DirName, IWebHostEnvironment _env)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }
            string DirPath = Path.Combine(_env.ContentRootPath, DirName);
            // 確保目錄存在
            if (!string.IsNullOrEmpty(DirPath) && !Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }
            string FileName = Guid.NewGuid().ToString();

            // 使用 FileStream 以非同步方式寫入
            string targetPath = Path.Combine(DirPath, FileName + Path.GetExtension(file.FileName));
            using (var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await file.CopyToAsync(stream);
                return FileName + Path.GetExtension(file.FileName);
            }
        }

        /// <summary>
        ///  刪除檔案
        /// </summary>
        /// <param name="filePath"></param>
        public static void DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static List<UISetting> GetUISetting()
        {
            var list = GetUISettingData();
            if (list.Count == 0)
                list = GetDefaultUISettingData();

            return list;
        }

        static List<UISetting> GetDefaultUISettingData()
        {
            string json = System.IO.File.ReadAllText("DefaultUISetting.json");
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UISetting>>(json);
            return result;
        }
        static List<UISetting> GetUISettingData()
        {
            string json = System.IO.File.ReadAllText("UISetting\\UISetting.json");
            if (!string.IsNullOrEmpty(json))
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UISetting>>(json);
                return result;
            }

            return new List<UISetting>();
        }

        public static string GetAppSettingsDataByName(string columnName)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, false)
                .AddJsonFile("message.json", true, false).Build();
            if (config[columnName] != null)
            {
                return config[columnName];
            }

            return string.Empty;
        }


        //public static List<SelectListItem> GetStatus(bool isIncludeCancel = true)
        //{
        //    //1:啟用 8:停用 9:作廢
        //    List<SelectListItem> list = new List<SelectListItem>();
        //    ConvertUtility.Enum2Dictionary<StatusEnum>().Select(d => new SelectListItem() { Value = d.Key.ToString(), Text = d.Value }).ToList().ForEach(d => list.Add(d));
        //    return list;
        //}


        public static bool DictionaryEquals<TKey, TValue>(Dictionary<TKey, TValue> dic1, Dictionary<TKey, TValue> dic2)
        {
            if (ReferenceEquals(dic1, dic2)) return true;
            if (dic1 == null || dic2 == null) return false;
            if (dic1.Count != dic2.Count) return false;

            foreach (var kvp in dic1)
            {
                if (!dic2.TryGetValue(kvp.Key, out var value) ||
                    !EqualityComparer<TValue>.Default.Equals(kvp.Value, value))
                {
                    return false;
                }
            }

            return true;
        }
        public static string FileExtension2MIMEType(string type)
        {
            string result = "";
            switch (type)
            {
                case ".xls":
                    result = "application/vnd.ms-excel";
                    break;
                case ".xlsx":
                    result = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                case ".ods":
                    result = "application/vnd.oasis.opendocument.spreadsheet";
                    break;
                case ".pdf":
                    result = "application/pdf";
                    break;
                case ".csv":
                    result = "text/csv";
                    break;
                case ".jpeg":
                case ".jpg":
                    result = "image/jpeg";
                    break;
                case ".png":
                    result = "image/png";
                    break;
                case ".zip":
                    result = "application/zip";
                    break;
                case ".doc":
                    result = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                case ".docx":
                    result = "application/msword";
                    break;
                case ".json":
                    result = "application/json";
                    break;
                case ".ppt":
                    result = "application/vnd.ms-powerpoint";
                    break;
                case ".pptx":
                    result = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                    break;
                case ".gif":
                    result = "image/gif";
                    break;
            }

            return result;
        }

        #region 星期、西元日期 <=> JSON 的方法
        /// <summary>
        /// 將輸入的星期字串（如 "星期一", "Mon", "Monday" 等）解析為對應的數字（1-7），並以 JSON 格式返回這些數字的列表。
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ParseWeekdaysToJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "星期一",1 }, { "週一",1 }, { "礼拜一",1 }, { "禮拜一",1 }, { "Mon",1 }, { "Monday",1 },
            { "星期二",2 }, { "週二",2 }, { "礼拜二",2 }, { "禮拜二",2 }, { "Tue",2 }, { "Tuesday",2 },
            { "星期三",3 }, { "週三",3 }, { "礼拜三",3 }, { "禮拜三",3 }, { "Wed",3 }, { "Wednesday",3 },
            { "星期四",4 }, { "週四",4 }, { "礼拜四",4 }, { "禮拜四",4 }, { "Thu",4 }, { "Thursday",4 },
            { "星期五",5 }, { "週五",5 }, { "礼拜五",5 }, { "禮拜五",5 }, { "Fri",5 }, { "Friday",5 },
            { "星期六",6 }, { "週六",6 }, { "礼拜六",6 }, { "禮拜六",6 }, { "Sat",6 }, { "Saturday",6 },
            { "星期日",7 }, { "週日",7 }, { "星期天",7 }, { "週天",7 },
            { "礼拜日",7 }, { "禮拜日",7 }, { "Sunday",7 }, { "Sun",7 }
        };

            var result = new HashSet<int>();

            foreach (var key in map.Keys)
            {
                if (Regex.IsMatch(input, Regex.Escape(key), RegexOptions.IgnoreCase))
                {
                    result.Add(map[key]);
                }
            }

            if (result.Count == 0)
                return null;

            var sorted = result.OrderBy(x => x).ToList();

            return JsonSerializer.Serialize(sorted);
        }


        /// <summary>
        /// 將JSON 轉為對應的星期文字（如 "週一", "週二" 等），並以 "、" 分隔返回這些文字的列表。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string ConvertJsonToWeekdayText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var list = JsonSerializer.Deserialize<List<int>>(json);

                if (list == null || list.Count == 0)
                    return null;

                var map = new Dictionary<int, string>
            {
                {1,"週一"},
                {2,"週二"},
                {3,"週三"},
                {4,"週四"},
                {5,"週五"},
                {6,"週六"},
                {7,"週日"}
            };

                var validDays = list
                    .Where(x => map.ContainsKey(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => map[x])
                    .ToList();

                if (validDays.Count == 0)
                    return null;

                return string.Join("、", validDays);
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// 從文字中擷取多個西元日期並轉為 JSON
        /// 輸出格式: ["2026-02-26","2026-03-01"]
        /// 若沒有有效日期回傳 null
        /// </summary>
        public static string ParseAdDatesToJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var pattern =
                @"(?<y>\d{4})\s*(?:年|[\/\-.])\s*(?<m>\d{1,2})\s*(?:月|[\/\-.])\s*(?<d>\d{1,2})\s*(?:日)?";

            var matches = Regex.Matches(input, pattern);

            var set = new HashSet<DateTime>();

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                if (!int.TryParse(match.Groups["y"].Value, out int year))
                    continue;

                if (!int.TryParse(match.Groups["m"].Value, out int month))
                    continue;

                if (!int.TryParse(match.Groups["d"].Value, out int day))
                    continue;

                if (IsValidDate(year, month, day))
                {
                    set.Add(new DateTime(year, month, day));
                }
            }

            if (set.Count == 0)
                return null;

            var sorted = set
                .OrderBy(x => x)
                .Select(x => x.ToString("yyyy-MM-dd"))
                .ToList();

            return JsonSerializer.Serialize(sorted);
        }


        private static bool IsValidDate(int y, int m, int d)
        {
            if (m < 1 || m > 12)
                return false;

            int days;

            try
            {
                days = DateTime.DaysInMonth(y, m);
            }
            catch
            {
                return false;
            }

            return d >= 1 && d <= days;
        }


        /// <summary>
        /// JSON轉西元日期顯示字串
        /// 輸出: 2026-02-26、2026-03-01
        /// 無有效資料回傳 null
        /// </summary>
        public static string ConvertJsonToAdDateText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json);

                if (list == null || list.Count == 0)
                    return null;

                var valid = new List<DateTime>();

                foreach (var item in list)
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;

                    var match = Regex.Match(
                        item,
                        @"^(?<y>\d{4})[-\/\.](?<m>\d{1,2})[-\/\.](?<d>\d{1,2})$");

                    if (!match.Success)
                        continue;

                    int y = int.Parse(match.Groups["y"].Value);
                    int m = int.Parse(match.Groups["m"].Value);
                    int d = int.Parse(match.Groups["d"].Value);

                    if (IsValidDate(y, m, d))
                    {
                        valid.Add(new DateTime(y, m, d));
                    }
                }

                if (valid.Count == 0)
                    return null;

                var result = valid
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => x.ToString("yyyy-MM-dd"))
                    .ToList();

                return string.Join("、", result);
            }
            catch
            {
                return null;
            }
        }
        #endregion


        public static List<SelectListItem> GetAccountStatus()
        {
            return GetStatus(false);
        }

        public static List<SelectListItem> GetStatus(bool isIncludeCancel = true)
        {
            //1:啟用 8:停用 9:作廢
            List<SelectListItem> list = new List<SelectListItem>();
            ConvertUtility.Enum2Dictionary<StatusEnum>().Select(d => new SelectListItem() { Value = d.Key.ToString(), Text = d.Value }).ToList().ForEach(d => list.Add(d));
            if (!isIncludeCancel)
            {
                int cancelOptionIdx = list.FindIndex(x => x.Value == StatusEnum.Cancel.ToInt().ToString());
                if (cancelOptionIdx > -1)
                {
                    list.RemoveAt(cancelOptionIdx);
                }
            }
            return list;
        }

    }
}
