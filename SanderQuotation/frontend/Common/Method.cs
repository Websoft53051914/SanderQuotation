using Core.Utility.Extensions;
using Core.Utility.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Const.Enums;

namespace frontend.Common
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

        public static List<SelectListItem> GetAccountStatus()
        {
            return GetStatus(false);
        }

        public static List<SelectListItem> GetAccountStatusForCreate()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "3", Text = "開通中" });
            return list;
        }

        public static List<SelectListItem> GetStatus(bool isIncludeCancel = true)
        {
            //1:啟用 8:停用 9:作廢
            List<SelectListItem> list = new List<SelectListItem>();
            Core.Utility.Utility.ConvertUtility.Enum2Dictionary<StatusEnum>().Select(d => new SelectListItem() { Value = d.Key.ToString(), Text = d.Value }).ToList().ForEach(d => list.Add(d));
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
                return "信件發送失敗" + ex.Message;
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
        public static void SetToSession( Controllers.HomeController.LoginDM loginDM)
        {
            DateTime dtTime;
            var _Current = LoginSession.Current;
            _Current.AccountId = loginDM.Id;
            _Current.AccountName = loginDM.AccountName;
            _Current.Account = loginDM.MemberAccount;

            _Current.TempAccount = loginDM.TempAccount;

            LoginSession.Current = _Current;
        }

        public static void SetToSession(SessionVO vo)
        {
            LoginSession.Current = vo;

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
            ///建立字母對應表(A~Z)
            ///A=10 B=11 C=12 D=13 E=14 F=15 G=16 H=17 J=18 K=19 L=20 M=21 N=22
            ///P=23 Q=24 R=25 S=26 T=27 U=28 V=29 X=30 Y=31 W=32  Z=33 I=34 O=35 
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
            ///建立字母對應表(A~Z)
            ///A=10 B=11 C=12 D=13 E=14 F=15 G=16 H=17 J=18 K=19 L=20 M=21 N=22
            ///P=23 Q=24 R=25 S=26 T=27 U=28 V=29 X=30 Y=31 W=32  Z=33 I=34 O=35 
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
            catch (System.Exception oEx)
            {
                oResult.IsTrue = false;
                oResult.cMessage = oEx.Message;
            }
            return oResult;
        }
        public static string GetClientIPAddress()
        {
            var context = HttpContext.Current;
            string ClientIP = context.GetServerVariable("HTTP_X_FORWARDED_FOR");
            if (String.IsNullOrEmpty(ClientIP))
            {
                ClientIP = context.GetServerVariable("REMOTE_ADDR")?.ToString() ?? "::1";
            }
            ClientIP = ClientIP.Replace("::1", "127.0.0.1");
            return ClientIP;
        }

        public static bool IsUploadFileExtensionValid(string extension)
        {
            return extension == ".jpg" || extension == ".jpeg"
                || extension == ".pdf"
                || extension == ".odt" || extension == ".ods"
                || extension == ".xls" || extension == ".xlsx"
                || extension == ".doc" || extension == ".docx"
                || extension == ".ppt" || extension == ".pptx";
        }



        public static string MixUnicodeToString(string mixUnicode)
        {
            byte[] textBytes = Encoding.Unicode.GetBytes(mixUnicode);
            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.UTF8, textBytes));
        }

        internal static List<SelectListItem> GetClassTypeList()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Value = "1", Text = "移工一站式" });
            list.Add(new SelectListItem() { Value = "2", Text = "關懷服務" });
            return list;
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


        //public static string SaveFile(IFormFile file, out string readLocation)
        //{
        //    Guid fileId = Guid.NewGuid();
        //    readLocation = @"../" + System.Configuration.ConfigurationManager.AppSettings["FileLocation"] + $"/{fileId.ToString()}{System.IO.Path.GetExtension(file.FileName)}";
        //    var lo = System.IO.Path.GetFullPath(System.Configuration.ConfigurationManager.AppSettings["FileLocation"]);

        //    //if (file.Length > 0)
        //    {
        //        var loc = $@"{lo}\{fileId.ToString()}{System.IO.Path.GetExtension(file.FileName)}";
        //        loc = loc.Replace("..", "");

        //        using (var stream = System.IO.File.Create(loc))
        //        {
        //            file.CopyTo(stream);
        //        }

        //        return loc;
        //    }

        //    //return null;
        //}

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
        private static bool IsPrivateServer()
        {
            string fileServer = System.Configuration.ConfigurationManager.AppSettings["FileServer"];
            bool isPrivateServer = fileServer != null && "PRIVATE".Equals(fileServer.ToUpper());

            return isPrivateServer;
        }

        //20251218為了解決部署在linux後，一段時間會出現異常訊息，系統就掛掉
            //IOException: The configured user limit (512) on the number of inotify instances has been reached,
            //or the per-process limit on the number of open file descriptors has been reached.
        
        //20251218 1.在 Method 類別中新增一個私有的靜態變數
        private static IConfiguration _cachedConfig;

        public static string GetAppSettingsDataByName(string columnName)
        {
            //20251218  2. 檢查是否已經 Build 過，如果沒有才 Build
            if (_cachedConfig == null)
            {
                _cachedConfig = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    // 將 reloadOnChange 設為 false (最安全) 
                    // 或者保留 true，但因為只執行一次，所以只會佔用 1 個 inotify
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();
            }

            //20251218 3. 永遠從快取中讀取資料
            if (_cachedConfig[columnName] != null)
            {
                return _cachedConfig[columnName];
            }
            //20251218
            //IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            //if (config[columnName] != null)
            //{
            //    return config[columnName];
            //}

            return string.Empty;
        }

        public static string CalculateGrowthRate(int current, int previous)
        {
            if (current == previous)
            {
                return "0%";
            }
            if (current != 0 && previous == 0)
            {
                return "";
            }
            if (current == 0 && previous != 0)
            {
                return "-100%";
            }
            var growthRate = ((decimal)current - previous) / previous * 100;
            return $"{Math.Round(growthRate, 2)}%";
        }

        public static string CalculateGrowthRate(int current, int previous, out bool? Positive)
        {
            if (current == previous)
            {
                Positive = null;
                return "0%";
            }
            if (current != 0 && previous == 0)
            {
                Positive = null;
                return "";
            }
            if (current == 0 && previous != 0)
            {
                Positive = false;
                return "-100%";
            }
            var growthRate = ((decimal)current - previous) / previous * 100;
            Positive = growthRate > 0;
            return $"{Math.Round(growthRate, 2)}%";
        }

    }
}
