using backend.Common;
using Business.BusinessLogic;
using Business.DomainModel;
using Core.Utility.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.Json;
using ViewModel;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseProjectController
    {
        private const string NasCredentialEncryptedPrefix = "ENC::";
        private readonly AdAuthService _ad;
        private readonly JwtService _jwt;
        private readonly IConfiguration _config;

        /// <summary>
        /// 功能說明：建立驗證 Controller，注入 AD 驗證、JWT 與組態。
        /// </summary>
        /// <param name="ad">輸入參數：Active Directory 驗證服務。</param>
        /// <param name="jwt">輸入參數：JWT 產生服務。</param>
        /// <param name="config">輸入參數：應用程式組態。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController — 基底與 GetMsg。
        /// 訊息內容及生成條件：建構子本身不產生 API 回應。
        /// </remarks>
        public AuthController(AdAuthService ad, JwtService jwt,  IConfiguration config):base(config)
        {
            _ad = ad;
            _jwt = jwt;
            _config = config;
        }

//        [HttpPost("login")]
//        public async Task<IActionResult> Login(LoginDto dto)
//        {
//            try
//            {
//                var msg = "";
//                var result = false;

//                Dictionary<string, string> userInfo = new Dictionary<string, string>();

//                //debug直接略過AD檢查
//#if DEBUG
//                {
//                    result = true;

//                    userInfo["AccessToken"] = "AccessToken";
//                    userInfo["UserAccount"] = "UserAccount";
//                    userInfo["CompanyID"] = "CompanyID";
//                    userInfo["ExpireAt"] = "ExpireAt";
//                    userInfo["UserName"] = "UserName";
//                }
//#else

//                result = _ad.Validate(new AdAuthService.ADInfo()
//                {
//                    Server = "192.168.1.210",
//                    BaseDN = "DC=mupoc,DC=local",
//                    Domain = "MUPOC",
//                    Port = 389
//                },
//                dto.Username, dto.Password, out userInfo, out msg);

//#endif

//                //AD驗證成功 發JWT
//                if (result)
//                {
//                    var safeToken = Common.Method.CookieSafeEncode(_jwt.Generate(userInfo, out var jwtExpiresUtc));
//                    AppendJwtCookie(safeToken, jwtExpiresUtc);

//                    //TODO 後續追加token存入table對應帳號

//                    string testConfig = @"{
//  ""Data"": {
//    ""AccessToken"": ""BC1502597C7E4DB9A029C52D741B257D"",
//    ""CompanyID"": ""MU"",
//    ""UserAccount"": ""Muproject"",
//    ""ExpireAt"": ""2026-01-30T22:07:38.250"",
//    ""RoleList"": [
//      ""Administrators"",
//      ""Employees"",
//      ""FinanceManagers""
//    ],
//    ""DepList"": [
//      ""D01"",
//      ""D02""
//    ],
//    ""NASData"": {
//      ""NASCompanyType"": ""synology"",
//      ""NASCompanyUrl"": ""eip.megaunion.com.tw:5001"",
//      ""NASCompanyPath"": ""\\公司\\@CompanyID\\"",
//      ""NASDepType"": ""synology"",
//      ""NASDepUrl"": ""eip.megaunion.com.tw:5001"",
//      ""NASDepPath"": ""\\部門\\@CompanyID\\@DepID\\"",
//      ""NASAccountType"": ""synology"",
//      ""NASAccountUrl"": ""eip.megaunion.com.tw:5001"",
//      ""NASAccountPath"": ""\\個人資料夾\\@UserAccount\\"",
//      ""NASProjectType"": ""synology"",
//      ""NASProjectUrl"": ""eip.megaunion.com.tw:5001"",
//      ""NASProjectPath"": ""\\專案\\@CompanyID\\@ProjectID\\""
//    }
//  }
//}";
//                    using var doc = JsonDocument.Parse(testConfig);
//                    var loginResponse = JsonSerializer.Deserialize<LoginResponseVO>(doc.RootElement.GetProperty("Data").GetRawText());
//                    //設定NAS Cookie

//#if DEBUG
//#else
//                    try
//                    {
//                        await NasLogin(loginResponse!, Response, "Muproject", "Muproject2026");
//                    }
//                    catch (Exception ex)
//                    {
//                        LogError(ex);
//                        return Unauthorized(new { success = false, message = GetMsg(_config, "NAS_verification_failed") + "：" + ex });
//                    }
//#endif

//                    return Ok(new
//                    {
//                        success = true,
//                        //token,
//                        user = userInfo
//                    });
//                }

//                if (!string.IsNullOrEmpty(msg))
//                {
//                    return Unauthorized(new { success = false, message = msg });
//                }
//            }
//            catch (Exception ex)
//            {
//                LogError(ex);
//                return Unauthorized(new { success = false, message = GetMsg(_config, "AD_verification_failed") + "：" + ex });
//            }

//            return Unauthorized(new { success = false, message = GetMsg(_config, "AD_verification_failed") });
//        }

        //private async Task NasLogin(LoginResponseVO loginResponse, HttpResponse response, string ac, string ac2)
        //{
        //    var nasData = loginResponse.NASData;


        //    List<string> urlDistinctList = new List<string>()
        //    {
        //        nasData.NASAccountUrl,
        //        nasData.NASCompanyUrl,
        //        nasData.NASDepUrl,
        //        nasData.NASProjectUrl
        //    }.Distinct().ToList();

        //    Dictionary<string, string> urlSidMap = new Dictionary<string, string>();
        //    foreach (var url in urlDistinctList)
        //    {
        //        var login = await _nasHelper.LoginAsync($"https://{url}", ac, ac2);
        //        urlSidMap[url!] = login.Sid;
        //    }

        //    string key = _config.GetValue<string>("SecretKey");
        //    string iv = _config.GetValue<string>("SecretIV");
        //    string encryptedAccount = EncryptNasCredentialField(ac, key, iv);
        //    string encryptedPassword = EncryptNasCredentialField(ac2, key, iv);

        //    NasCookieValue accountNasCookieValue = new NasCookieValue()
        //    {
        //        FolderType = nasData.NASAccountType,
        //        Url = nasData.NASAccountUrl,
        //        Path = ReplacePathParameters(nasData.NASAccountPath, loginResponse.CompanyID, loginResponse.UserAccount),
        //        Sid = urlSidMap[nasData.NASAccountUrl!],
        //        Account = encryptedAccount,
        //        Password = encryptedPassword
        //    };

        //    response.Cookies.Append(NasCookieKey.AccountFolder, SecurityUtility.Encrypt(System.Text.Json.JsonSerializer.Serialize(accountNasCookieValue), key, iv), new CookieOptions
        //    {
        //        HttpOnly = true,
        //        Secure = true,
        //        SameSite = SameSiteMode.None,
        //        Path = "/",
        //    });

        //    NasCookieValue companyNasCookieValue = new NasCookieValue()
        //    {
        //        FolderType = nasData.NASCompanyType,
        //        Url = nasData.NASCompanyUrl,
        //        Path = ReplacePathParameters(nasData.NASCompanyPath, loginResponse.CompanyID, loginResponse.UserAccount),
        //        Sid = urlSidMap[nasData.NASCompanyUrl!],
        //        Account = encryptedAccount,
        //        Password = encryptedPassword
        //    };
        //    response.Cookies.Append(NasCookieKey.CompanyFolder, SecurityUtility.Encrypt(System.Text.Json.JsonSerializer.Serialize(companyNasCookieValue), key, iv), new CookieOptions
        //    {
        //        HttpOnly = true,
        //        Secure = true,
        //        SameSite = SameSiteMode.None,
        //        Path = "/"
        //    });

        //    NasCookieValue depNasCookieValue = new NasCookieValue()
        //    {
        //        FolderType = nasData.NASDepType,
        //        Url = nasData.NASDepUrl,
        //        Path = ReplacePathParameters(nasData.NASDepPath, loginResponse.CompanyID, loginResponse.UserAccount),
        //        Sid = urlSidMap[nasData.NASDepUrl!],
        //        Account = encryptedAccount,
        //        Password = encryptedPassword
        //    };
        //    response.Cookies.Append(NasCookieKey.DeptFolder, SecurityUtility.Encrypt(System.Text.Json.JsonSerializer.Serialize(depNasCookieValue), key, iv), new CookieOptions
        //    {
        //        HttpOnly = true,
        //        Secure = true,
        //        SameSite = SameSiteMode.None,
        //        Path = "/"
        //    });

        //    NasCookieValue projectNasCookieValue = new NasCookieValue()
        //    {
        //        FolderType = nasData.NASProjectType,
        //        Url = nasData.NASProjectUrl,
        //        Path = ReplacePathParameters(nasData.NASProjectPath, loginResponse.CompanyID, loginResponse.UserAccount),
        //        Sid = urlSidMap[nasData.NASProjectUrl!],
        //        Account = encryptedAccount,
        //        Password = encryptedPassword
        //    };
        //    response.Cookies.Append(NasCookieKey.ProjectFolder, SecurityUtility.Encrypt(System.Text.Json.JsonSerializer.Serialize(projectNasCookieValue), key, iv), new CookieOptions
        //    {
        //        HttpOnly = true,
        //        Secure = true,
        //        SameSite = SameSiteMode.None,
        //        Path = "/"
        //    });
        //}

        //private static string EncryptNasCredentialField(string value, string key, string iv)
        //{
        //    if (string.IsNullOrEmpty(value))
        //        return string.Empty;

        //    return NasCredentialEncryptedPrefix + SecurityUtility.Encrypt(value, key, iv);
        //}

        //        private string ReplacePathParameters(string pathTemplate, string companyId, string userAccount)
        //        {
        //            if (string.IsNullOrEmpty(pathTemplate))
        //                return string.Empty;

        //            // \\改成 /
        //            pathTemplate = pathTemplate.Replace("\\", "/");

        //            string result = pathTemplate
        //                .Replace("@CompanyID", companyId ?? string.Empty)
        //                .Replace("@UserAccount", userAccount ?? string.Empty);

        //            result = result.TrimEnd('/');

        //            return result;
        //        }

        //        [Authorize] //驗證JWT
        //        [HttpGet("profile")]
        //        public IActionResult Profile()
        //        {
        //            LoginBL bl = BLFactory.GetInstance<LoginBL>();
        //            LogError("錯誤訊息");
        //            return Ok(new
        //            {
        //                name = User.Identity?.Name,
        //                email = User.FindFirstValue(ClaimTypes.Email)
        //            });
        //        }

        //        [HttpGet("AuthDoLoad")]
        //        [AllowAnonymous] // 允許匿名訪問，登入前需要使用
        //        public IActionResult AuthDoLoad()
        //        {
        //            try
        //            {
        //                LoginBL bl = BLFactory.GetInstance<LoginBL>();
        //                var result = bl.AuthDoLoad(Common.Method.GetClientIPAddress(), Request.Headers["User-Agent"].ToString());

        //                if (!string.IsNullOrEmpty(result.ErrorMsg))
        //                    return JsonValidFail(result.ErrorMsg);

        //                return JsonSuccess(result);
        //            }
        //            catch (Exception ex)
        //            {
        //                LogError(ex);
        //                return JsonValidFail(GetMsg(_config, "System_Error"));
        //            }
        //        }

        /// <summary>
        /// 功能說明：使用者登入驗證，成功後寫入 JWT Cookie 並回傳登入資料。
        /// </summary>
        /// <param name="vm">輸入參數：UserName、Password 等登入表單。</param>
        /// <returns>輸出參數：JsonSuccess(LoginDM)；鎖定或驗證失敗時 JsonValidFail；例外時 System_Error。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：LoginBL.CheckLock — 帳號鎖定檢查；AuthDoAuth — 驗證帳密；CookiesAppend — 產生 JWT Cookie；LogSuccess。
        /// 訊息內容及生成條件：CheckLock 錯誤 → JsonValidFail(BL 訊息)；AuthDoAuth 錯誤 → JsonValidFail；成功 → JsonSuccess(loginDM) 並 LogSuccess「登入成功」；catch → System_Error。
        /// </remarks>
        [HttpPost("AuthDoPost")]
        [AllowAnonymous] // 允許匿名訪問，登入前需要使用
        public async Task<IActionResult> AuthDoPost(LoginVM vm)
        {
            try
            {
                var msg = "";

                Dictionary<string, string> userInfo = new Dictionary<string, string>();


                LoginBL bl = BLFactory.GetInstance<LoginBL>();
                bl.CheckLock(new LoginDM()
                {
                    MemberAccount = vm.UserName,
                });
                if (bl.GetMessage().IsError())
                {
                    msg = bl.GetMessage().GetErrMsg();
                    return JsonValidFail(msg);
                }
                LoginDM? loginDM = bl.AuthDoAuth(vm.UserName, vm.Password);
                //result.Data.UserAccount = vm.UserName; //後續NAS登入需要
                //result.Data.Password = vm.Password; //後續NAS登入需要

                if (bl.GetMessage().IsError())
                {
                    msg = bl.GetMessage().GetErrMsg();
                    return JsonValidFail(msg);
                }

                //try
                //{
                //    var nasLogin = Common.Method.GetAppSettingsDataByName("IsNasLogin") ?? "true";
                //    if (nasLogin.ToLower() == "true")
                //    {

                //        await NasLogin(new LoginResponseVO()
                //        {
                //            CompanyID = vm.CompanyData.CompanyID,
                //            UserAccount = result.Data.UserAccount,
                //            NASData = new NASDataVO()
                //            {
                //                NASAccountType = vm.CompanyData.NASAccountType,
                //                NASAccountUrl = vm.CompanyData.NASAccountUrl,
                //                NASAccountPath = vm.CompanyData.NASAccountPath,
                //                NASCompanyType = vm.CompanyData.NASCompanyType,
                //                NASCompanyUrl = vm.CompanyData.NASCompanyUrl,
                //                NASCompanyPath = vm.CompanyData.NASCompanyPath,
                //                NASDepType = vm.CompanyData.NASDepType,
                //                NASDepUrl = vm.CompanyData.NASDepUrl,
                //                NASDepPath = vm.CompanyData.NASDepPath,
                //                NASProjectType = vm.CompanyData.NASProjectType,
                //                NASProjectUrl = vm.CompanyData.NASProjectUrl,
                //                NASProjectPath = vm.CompanyData.NASProjectPath
                //            }
                //        }, Response, vm.UserName, vm.Password);
                //    }
                //}
                //catch (Exception exx)
                //{
                //    LogError(GetMsg(_config, "NAS_Login_Error") + "：" + exx);
                //}

                CookiesAppend(loginDM);

                LogSuccess("登入成功");
                return JsonSuccess(loginDM);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }


        //        [HttpPost("Auth2DoLoad")]
        //        [AllowAnonymous] // 允許匿名訪問，登入前需要使用
        //        public IActionResult Auth2DoLoad(LoginVM vm)
        //        {
        //            try
        //            {
        //                LoginBL bl = BLFactory.GetInstance<LoginBL>();
        //                var result = bl.Auth2DoLoad(vm.CompanyData.CompanyID, vm.TokenValue, Common.Method.GetClientIPAddress(), Request.Headers["User-Agent"].ToString());

        //                if (!string.IsNullOrEmpty(result.ErrorMsg))
        //                    return JsonValidFail(result.ErrorMsg);

        //                return JsonSuccess(result.DispatcherReturnMsg.ReturnMsg);
        //            }
        //            catch (Exception ex)
        //            {
        //                LogError(ex);
        //                return JsonValidFail(GetMsg(_config, "System_Error"));
        //            }
        //        }

        /// <summary>
        /// 功能說明：將 JWT 寫入 HttpOnly Cookie（依 HTTPS 調整 Secure/SameSite）。
        /// </summary>
        /// <param name="safeToken">輸入參數：經 CookieSafeEncode 編碼後的 Token。</param>
        /// <param name="jwtExpiresUtc">輸入參數：Token 到期 UTC 時間。</param>
        /// <remarks>
        /// 參考功能名稱與用途：Const.Value.JWT_TokenName — Cookie 名稱；Request.IsHttps — 決定 Secure 與 SameSite。
        /// 訊息內容及生成條件：無 JSON 回應，僅設定 Response.Cookies。
        /// </remarks>
        private void AppendJwtCookie(string safeToken, DateTime jwtExpiresUtc)
        {
            var domain = Common.Method.GetAppSettingsDataByName("frontendDoamin");
#if DEBUG
            domain = "";
#endif

            // HTTP 環境：Secure=false、SameSite=Lax
            // HTTPS 環境：Secure=true、SameSite=None（跨站）
            var isHttps = Request.IsHttps;
            Response.Cookies.Append(Const.Value.JWT_TokenName, safeToken, new CookieOptions
            {
                //Domain = domain,
                HttpOnly = true,
                Secure = isHttps,
                SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
                Expires = new DateTimeOffset(jwtExpiresUtc)
            });
        }

        /// <summary>
        /// 功能說明：依登入結果產生 JWT 並寫入 Cookie。
        /// </summary>
        /// <param name="result">輸入參數：LoginDM（含 MemberAccount、PermissionCodeList 等）。</param>
        /// <remarks>
        /// 參考功能名稱與用途：JwtService.Generate — 產生 Token；AppendJwtCookie — 寫入 Cookie。
        /// 訊息內容及生成條件：無 JSON 回應。
        /// </remarks>
        private void CookiesAppend(LoginDM result)
        {
            Dictionary<string, string> userInfo = new Dictionary<string, string>();

            userInfo["UserAccount"] = result.MemberAccount;
            userInfo["UserName"] = result.AccountName;
            userInfo["PermissionCodeList"] = JsonConvert.SerializeObject(result.PermissionCodeList);

            var safeToken = Common.Method.CookieSafeEncode(_jwt.Generate(userInfo, out var jwtExpiresUtc));
            AppendJwtCookie(safeToken, jwtExpiresUtc);
        }

        //        [HttpPost("Auth2DoPost")]
        //        [AllowAnonymous] // 允許匿名訪問，登入前需要使用
        //        public IActionResult Auth2DoPost(LoginVM vm)
        //        {
        //            try
        //            {
        //                LoginBL bl = BLFactory.GetInstance<LoginBL>();
        //                var result = bl.Auth2DoAuth(vm.CompanyData.CompanyID, vm.TokenValue, vm.VerifyCode, Common.Method.GetClientIPAddress(), Request.Headers["User-Agent"].ToString());

        //                if (!string.IsNullOrEmpty(result.ErrorMsg))
        //                    return JsonValidFail(result.ErrorMsg);

        //                result.Data.Password = vm.Password;
        //                CookiesAppend(result);

        //                return JsonSuccess(result);
        //            }
        //            catch (Exception ex)
        //            {
        //                LogError(ex);
        //                return JsonValidFail(GetMsg(_config, "System_Error"));
        //            }
        //        }

        //        [HttpPost("PINCodeDoUpdate")]
        //        [Authorize]
        //        public IActionResult PINCodeDoUpdate(PinCode pincode)
        //        {
        //            try
        //            {
        //                LoginBL bl = BLFactory.GetInstance<LoginBL>();
        //                var result = bl.PINCodeDoUpdate(pincode, UserInfo);
        //                if (!string.IsNullOrEmpty(result.ErrorMsg))
        //                    return JsonValidFail(result.ErrorMsg);

        //                return JsonSuccess(GetMsg(_config, "PINCode_Update_Success"));
        //            }
        //            catch (Exception ex)
        //            {
        //                LogError(ex);
        //                return JsonValidFail(GetMsg(_config, "System_Error"));
        //            }
        //        }

        /// <summary>
        /// 功能說明：使用者登出，清除伺服器端登入狀態。
        /// </summary>
        /// <returns>輸出參數：JsonSuccess("")；例外時 JsonValidFail(System_Error)。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：LoginBL.LogoutDoPost — 依 UserInfo 執行登出邏輯。
        /// 訊息內容及生成條件：成功 → JsonSuccess 空字串；例外 → System_Error。
        /// </remarks>
        [HttpPost("LogoutDoPost")]
        [Authorize]
        public IActionResult LogoutDoPost()
        {
            try
            {
                LoginBL bl = BLFactory.GetInstance<LoginBL>();
                bl.LogoutDoPost(UserInfo);

                return JsonSuccess("");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        //        [HttpPost("PINCodeDoSend")]
        //        [AllowAnonymous] // 允許匿名訪問，登入前需要使用
        //        public IActionResult PINCodeDoSend(LoginVM vm)
        //        {
        //            try
        //            {
        //                LoginBL bl = BLFactory.GetInstance<LoginBL>();
        //                var result = bl.PINCodeDoSend(
        //                    userName: vm.UserName,
        //                    tokenValue: vm.TokenValue,
        //                    companyID: vm.CompanyData.CompanyID,
        //                    ip: Common.Method.GetClientIPAddress(),
        //                    Request.Headers["User-Agent"].ToString()
        //                    );
        //                if (!string.IsNullOrEmpty(result.ErrorMsg))
        //                    return JsonValidFail(result.ErrorMsg);

        //                return JsonSuccess(result.DispatcherReturnMsg.ReturnMsg);
        //            }
        //            catch (Exception ex)
        //            {
        //                LogError(ex);
        //                return JsonValidFail(GetMsg(_config, "System_Error"));
        //            }
        //        }


    }

    public record LoginDto(string Username, string Password);

}