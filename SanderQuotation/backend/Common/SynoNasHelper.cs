using backend.Models.Cookie;
using Core.Utility.Utility;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using static CommonClass.Model.SynoNasTreeNodeDM;
using static Const.Enums;

namespace backend.Common
{
    public class SynoNasConnectionContext
    {
        public string Url { get; set; }
        public string Sid { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }

        public string RootFolderPath { get; set; }
    }

    public class SynoNasLoginResponse
    {
        public bool Success { get; set; }
        public string Sid { get; set; }
        public string Error { get; set; }
    }

    public class SynoNasApiException : Exception
    {
        public int NasErrorCode { get; }

        public SynoNasApiException(int code, string message)
            : base(message)
        {
            NasErrorCode = code;
        }
    }

    /// <summary>
    /// 攜帶 message.json Key 的 NAS 例外，供 Controller 查詢多語系訊息
    /// </summary>
    public class SynoNasMsgException : Exception
    {
        public string MessageKey { get; }

        public SynoNasMsgException(string messageKey)
            : base(messageKey)
        {
            MessageKey = messageKey;
        }
    }

    public partial class SynoNasHelper
    {
        private const string NasCredentialEncryptedPrefix = "ENC::";
        private class ApiInfo
        {
            public int maxVersion { get; set; }
            public int minVersion { get; set; }
            public string path { get; set; }
        }

        private readonly HttpClient _httpClient;
        //private readonly Dictionary<string, ApiInfo> _apiInfoCache = new Dictionary<string, ApiInfo>();
        private readonly IMemoryCache _cache;
        public SynoNasHelper(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }


        /// <summary>
        /// 登入並取得 SID
        /// </summary>
        /// <param name="url">NAS URL</param>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns></returns>
        public async Task<SynoNasLoginResponse> LoginAsync(string url, string account, string password)
        {
            string baseUrl = url.TrimEnd('/') + "/webapi";

            await GetApiInfo(baseUrl);

            ApiInfo info = await GetApiInfoSpecific("SYNO.API.Auth");
            var loginUrl = $"{baseUrl}/{info.path}?api=SYNO.API.Auth&version={info.maxVersion}&method=login&account={account}&passwd={password}&session=FileStation&format=sid";

            var response = await _httpClient.GetAsync(loginUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.GetProperty("success").GetBoolean())
                {
                    var sid = doc.RootElement.GetProperty("data").GetProperty("sid").GetString();
                    return new SynoNasLoginResponse
                    {
                        Success = true,
                        Sid = sid
                    };
                }
            }

            return new SynoNasLoginResponse
            {
                Success = false,
                Error = "登入 NAS 失敗"
            };
        }

        /// <summary>
        /// 從 NAS 取得所有 API 的資訊並快取起來
        /// </summary>
        /// <returns></returns>
        private async Task GetApiInfo(string baseUrl)
        {

            if (!_cache.TryGetValue("ApiInfoCache", out var e))
            {
                string apiInfo = await QueryApiAllAsync(baseUrl);
                using var doc = JsonDocument.Parse(apiInfo);
                if (doc.RootElement.GetProperty("success").GetBoolean())
                {
                    Dictionary<string, ApiInfo> apiInfoCache = new Dictionary<string, ApiInfo>();

                    var data = doc.RootElement.GetProperty("data");
                    foreach (var api in data.EnumerateObject())
                    {
                        var apiData = api.Value;
                        apiInfoCache[api.Name] = new ApiInfo
                        {
                            maxVersion = apiData.GetProperty("maxVersion").GetInt32(),
                            minVersion = apiData.GetProperty("minVersion").GetInt32(),
                            path = apiData.GetProperty("path").GetString()
                        };
                    }

                    _cache.Set("ApiInfoCache", apiInfoCache);
                }
            }

        }


        /// <summary>
        /// 查詢特定 API 的資訊（版本、路徑）
        /// </summary>
        /// <param name="baseUrl">基礎 URL</param>
        /// <param name = "apiNames" > 想要查詢的 API 名稱，例如 "SYNO.FileStation.List"</param>
        private async Task<string> QueryApiAsync(string baseUrl, params string[] apiNames)
        {
            // SYNO.API.Info 是少數不需要 SID 的 API
            string queryNames = string.Join(",", apiNames);
            var url = $"{baseUrl}/query.cgi?api=SYNO.API.Info&version=1&method=query&query={queryNames}";

            var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 查詢所有 API 的資訊（版本、路徑）
        /// </summary>
        /// <param name="baseUrl">基礎 URL</param>
        /// <returns></returns>
        private async Task<string> QueryApiAllAsync(string baseUrl)
        {
            // SYNO.API.Info 是少數不需要 SID 的 API
            var url = $"{baseUrl}/query.cgi?api=SYNO.API.Info&version=1&method=query&query=all";
            var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 查詢特定 API 的資訊（版本、路徑）
        /// </summary>
        /// <param name="apiName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<ApiInfo> GetApiInfoSpecific(string apiName)
        {
            if (_cache.TryGetValue("ApiInfoCache", out Dictionary<string, ApiInfo> apiInfoCache))
            {
                if (apiInfoCache.ContainsKey(apiName))
                {
                    return apiInfoCache[apiName];
                }
                else
                {
                    throw new SynoNasMsgException("Nas_Api_Not_Found");
                }
            }
            else
            {
                throw new SynoNasMsgException("Nas_Api_Cache_Missing");
            }
        }



        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <returns></returns>
        public async Task<bool> LogoutAsync(SynoNasConnectionContext context)
        {
            var content = await ExecuteGetAsync(context, "SYNO.API.Auth", "logout");
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// 確保 Sid 有效；若為空則用 Account/Password 重新登入（自動續期）
        /// </summary>
        private async Task EnsureSidAsync(SynoNasConnectionContext context)
        {
            //context.Sid = null;
            if (!string.IsNullOrEmpty(context.Sid)) return;

            if (string.IsNullOrEmpty(context.Account) || string.IsNullOrEmpty(context.Password))
                throw new SynoNasMsgException("Nas_Sid_Missing");

            var loginResult = await LoginAsync(context.Url, context.Account, context.Password);
            if (!loginResult.Success)
                throw new SynoNasMsgException("Nas_Sid_Missing");

            context.Sid = loginResult.Sid;
        }

        /// <summary>
        /// 通用的GET請求方法 
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="apiName"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> ExecuteGetAsync(SynoNasConnectionContext context, string apiName, string method, Dictionary<string, string> parameters = null)
        {
            try
            {
                await EnsureSidAsync(context);

                string baseUrl = context.Url.TrimEnd('/') + "/webapi";
                ApiInfo info = await GetApiInfoSpecific(apiName);
                var url = $"{baseUrl}/{info.path}?api={apiName}&version={info.maxVersion}&method={method}&_sid={context.Sid}";

                if (parameters != null)
                {
                    foreach (var param in parameters)
                        url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
                }

                using var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();

                return body;
            }
            catch (HttpRequestException e)
            {
                throw;
            }
        }
        /// <summary>
        /// 通用的GET請求方法 取得 Stream
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="apiName"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Stream> ExecuteGetSteamAsync(SynoNasConnectionContext context, string apiName, string method, Dictionary<string, string> parameters = null)
        {
            try
            {
                await EnsureSidAsync(context);

                string baseUrl = context.Url.TrimEnd('/') + "/webapi";
                ApiInfo info = await GetApiInfoSpecific(apiName);
                var url = $"{baseUrl}/{info.path}?api={apiName}&version={info.maxVersion}&method={method}&_sid={context.Sid}";

                if (parameters != null)
                {
                    foreach (var param in parameters)
                        url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
                }

                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                try
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStreamAsync();
                }
                catch
                {
                    response.Dispose();
                    throw;
                }
            }
            catch (HttpRequestException e)
            {
                throw;
            }
        }

        /// <summary>
        /// 通用的 POST 請求方法 
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="apiName"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> ExecutePostAsync(SynoNasConnectionContext context, string apiName, string method, Dictionary<string, string> parameters = null)
        {
            try
            {
                await EnsureSidAsync(context);

                string baseUrl = context.Url.TrimEnd('/') + "/webapi";
                ApiInfo info = await GetApiInfoSpecific(apiName);
                var url = $"{baseUrl}/{info.path}?api={apiName}&version={info.maxVersion}&method={method}&_sid={context.Sid}";

                var formContent = new FormUrlEncodedContent(parameters ?? new Dictionary<string, string>());
                using var response = await _httpClient.PostAsync(url, formContent);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw;
            }
        }

        /// <summary>
        /// 從 Cookie 取得 NAS 連線資訊（可供 Controller 外部呼叫）
        /// </summary>
        /// <param name="cookies">Request Cookie 集合</param>
        /// <param name="nasRequestType">NAS 請求類型</param>
        /// <param name="config">設定檔</param>
        /// <param name="extraCode">額外參數（ProjectFolder 需要 ProjectID，DepartmentFolder 需要 DepID）</param>
        /// <returns></returns>
        public static Task<SynoNasConnectionContext> CommonGetNasContext(
            IRequestCookieCollection cookies,
            NasRequestType nasRequestType,
            IConfiguration config,
            string extraCode = null)
        {
            string key = config.GetValue<string>("SecretKey");
            string iv = config.GetValue<string>("SecretIV");

            string cookieKey = string.Empty;

            switch (nasRequestType)
            {
                case NasRequestType.AccoountFolder:
                    cookieKey = NasCookieKey.AccountFolder;
                    break;
                case NasRequestType.ProjectFolder:
                    cookieKey = NasCookieKey.ProjectFolder;
                    break;
                case NasRequestType.DepartmentFolder:
                    cookieKey = NasCookieKey.DeptFolder;
                    break;
                case NasRequestType.CompanyFolder:
                    cookieKey = NasCookieKey.CompanyFolder;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(nasRequestType), nasRequestType, null);
            }

            if (cookies.TryGetValue(cookieKey, out string json))
            {
                string decryptedJson = SecurityUtility.Decrypt(json, key, iv);
                NasCookieValue cookieValue = JsonConvert.DeserializeObject<NasCookieValue>(decryptedJson);

                var context = new SynoNasConnectionContext()
                {
                    Url = $"https://{cookieValue.Url}",
                    Sid = cookieValue.Sid,
                    Account = DecryptNasCredentialField(cookieValue.Account, key, iv),
                    Password = DecryptNasCredentialField(cookieValue.Password, key, iv),
                    RootFolderPath = cookieValue.Path.StartsWith("/") ? cookieValue.Path : "/" + cookieValue.Path
                };

                switch (nasRequestType)
                {
                    case NasRequestType.ProjectFolder:
                        context.RootFolderPath = context.RootFolderPath.Replace("@ProjectID", extraCode ?? string.Empty);
                        break;
                    case NasRequestType.DepartmentFolder:
                        context.RootFolderPath = context.RootFolderPath.Replace("@DepID", extraCode ?? string.Empty);
                        break;
                    default:
                        break;
                }

                return Task.FromResult(context);
            }
            else
            {
                throw new SynoNasMsgException("Nas_Cookie_Not_Found");
            }
        }

        private static string DecryptNasCredentialField(string value, string key, string iv)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Backward compatibility: old cookie data stores plaintext fields.
            if (!value.StartsWith(NasCredentialEncryptedPrefix, StringComparison.Ordinal))
                return value;

            var encryptedPayload = value.Substring(NasCredentialEncryptedPrefix.Length);
            if (string.IsNullOrEmpty(encryptedPayload))
                return string.Empty;

            try
            {
                return SecurityUtility.Decrypt(encryptedPayload, key, iv);
            }
            catch
            {
                // Graceful fallback to avoid breaking existing sessions on malformed data.
                return value;
            }
        }


    }

    //FileStation
    public partial class SynoNasHelper
    {

        public class DirSizeStartData
        {
            [JsonProperty("taskid")]
            public string TaskId { get; set; } = "";
        }

        public class SynoResponse<T>
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("data")]
            public T? Data { get; set; }

            [JsonProperty("error")]
            public SynoError? Error { get; set; }
        }

        public class SynoError
        {
            [JsonProperty("code")]
            public int Code { get; set; }
        }

        /// <summary>
        /// [SYNO.FileStation.List - list] 列出指定目錄下的檔案和資料夾
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<SynoNasCommonResponse<FileStationListResponse>> FileStation_List(SynoNasConnectionContext context, FileStationListRequest model)
        {
            var res = new SynoNasCommonResponse<FileStationListResponse>();
            var @params = new Dictionary<string, string>
            {
                { "folder_path", model.FolderPath },
                { "offset", model.Offset.ToString() },
                { "limit", model.Limit.ToString() },
                { "sort_by", model.SortBy },
                { "sort_direction", model.SortDirection },
                { "filetype", model.Filetype },
                { "additional", JsonConvert.SerializeObject(model.Additional) }

            };
            //Task<SynoNasCommonResponse<FileStationDirSizeResponse>> test = Folder_DirSize(context, model.FolderPath);

            var content = await ExecuteGetAsync(context, "SYNO.FileStation.List", "list", @params);

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                var data = doc.RootElement.GetProperty("data");
                string jsonString = data.ToString();
                res.Data = JsonConvert.DeserializeObject<FileStationListResponse>(jsonString);
                res.Success = true;

            }
            else
            {
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                res.Success = false;
                res.CommonErrorCode = errCode;
                res.Error = $"{errCode}";
            }

            return res;
        }



        /// <summary>
        /// [SYNO.FileStation.List - list_share] 列出所有分享資料夾
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<SynoNasCommonResponse<FileStationShareListResponse>> FileStation_Share_List(SynoNasConnectionContext context, FileStationShareListRequest model)
        {
            var res = new SynoNasCommonResponse<FileStationShareListResponse>();
            var @params = new Dictionary<string, string>
            {
                { "offset", model.Offset.ToString() },
                { "limit", model.Limit.ToString() },
                { "sort_by", model.SortBy },
                { "sort_direction", model.SortDirection },
                { "onlywritable", model.OnlyWritable ? "true" : "false" },
                { "additional", JsonConvert.SerializeObject(model.Additional) }

            };
            var content = await ExecuteGetAsync(context, "SYNO.FileStation.List", "list_share", @params);

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                var data = doc.RootElement.GetProperty("data");
                string jsonString = data.ToString();
                res.Data = JsonConvert.DeserializeObject<FileStationShareListResponse>(jsonString);
                res.Success = true;
                return res;
            }
            else
            {
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                throw new Exception($"{errCode}");

            }
        }

        /// <summary>
        /// [FileStation.Download - download] 下載指定檔案
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<Stream> FileStation_Download(SynoNasConnectionContext context, FileStationDownloadRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "path", model.path },
                { "mode", "download" },
            };
            var content = await ExecuteGetSteamAsync(context, "SYNO.FileStation.Download", "download", @params);
            return content;
        }


        /// <summary>
        /// [FileStation.Delete - delete] 刪除指定檔案
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<SynoNasCommonResponse> FileStation_Delete(SynoNasConnectionContext context, FileStationDeleteRequest model)
        {
            SynoNasCommonResponse res = new SynoNasCommonResponse();
            var @params = new Dictionary<string, string>
            {
                { "path", JsonConvert.SerializeObject(model.path) },
                { "recursive", model.recursive ? "true" : "false" },
            };
            var content = await ExecuteGetAsync(context, "SYNO.FileStation.Delete", "delete", @params);
            using var doc = JsonDocument.Parse(content);
            bool success = doc.RootElement.GetProperty("success").GetBoolean();
            if (success)
            {
                res.Success = true;
            }
            else
            {
                res.Success = false;
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                if (errCode == "900")
                {
                    string detailedErrors = doc.RootElement.GetProperty("error").GetProperty("errors")[0].GetProperty("code").GetRawText();
                    res.CommonErrorCode = detailedErrors;
                }
                res.Error = $"{errCode}";
                //else
                //{
                //    res.Error = $"{errCode}";
                //}
            }

            return res;
        }


        /// <summary>
        /// [FileStation.Upload - upload] 上傳檔案到指定目錄
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public async Task<SynoNasCommonResponse> FileStation_Upload(SynoNasConnectionContext context, FileStationUploadRequest model, CancellationToken cancellationToken = default)
        {
            SynoNasCommonResponse res = new SynoNasCommonResponse();

            await EnsureSidAsync(context);
            string baseUrl = context.Url.TrimEnd('/') + "/webapi";
            ApiInfo info = await GetApiInfoSpecific("SYNO.FileStation.Upload");
            var url = $"{baseUrl}/{info.path}?api=SYNO.FileStation.Upload&version={info.maxVersion}&method=upload&_sid={context.Sid}";

            string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.ExpectContinue = false;

            var content = new MultipartFormDataStreamContent(boundary, model.DestFolderPath, model.FileName, model.FileContent);
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

            request.Content = content;

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);

            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                res.Success = true;
            }
            else
            {
                string errorCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                res.CommonErrorCode = errorCode;
                res.Error = $"{errorCode}";
            }
            return res;
        }

        private class MultipartFormDataStreamContent : HttpContent
        {
            private readonly string _boundary;
            private readonly string _destFolderPath;
            private readonly string _fileName;
            private readonly Stream _fileStream;

            public MultipartFormDataStreamContent(string boundary, string destFolderPath, string fileName, Stream fileStream)
            {
                _boundary = boundary;
                _destFolderPath = destFolderPath;
                _fileName = fileName;
                _fileStream = fileStream;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                var encoding = new UTF8Encoding(false);
                var writer = new StreamWriter(stream, encoding, 1024, leaveOpen: true);

                await writer.WriteAsync($"--{_boundary}\r\n");
                await writer.WriteAsync("Content-Disposition: form-data; name=\"path\"\r\n\r\n");
                await writer.WriteAsync($"{_destFolderPath}\r\n");

                await writer.WriteAsync($"--{_boundary}\r\n");
                await writer.WriteAsync("Content-Disposition: form-data; name=\"create_parents\"\r\n\r\n");
                await writer.WriteAsync("true\r\n");

                await writer.WriteAsync($"--{_boundary}\r\n");
                await writer.WriteAsync($"Content-Disposition: form-data; name=\"file\"; filename=\"{_fileName}\"\r\n");
                await writer.WriteAsync("Content-Type: application/octet-stream\r\n\r\n");
                await writer.FlushAsync();

                await _fileStream.CopyToAsync(stream, 81920);

                await writer.WriteAsync($"\r\n--{_boundary}--\r\n");
                await writer.FlushAsync();
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }

        /// <summary>
        /// 取得 FileStation Upload API 的完整 URL（供前端直接上傳）
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="destFolderPath">目標資料夾路徑</param>
        /// <returns></returns>
        public async Task<string> GetFileStationUploadUrl(SynoNasConnectionContext context, string destFolderPath)
        {
            await EnsureSidAsync(context);

            string baseUrl = context.Url.TrimEnd('/') + "/webapi";

            // 從快取取得 API 資訊
            if (!_cache.TryGetValue("ApiInfoCache", out Dictionary<string, ApiInfo> apiInfoCache))
            {
                throw new SynoNasMsgException("Nas_Api_Cache_Missing");
            }

            if (!apiInfoCache.ContainsKey("SYNO.FileStation.Upload"))
            {
                throw new SynoNasMsgException("Nas_Api_Not_Found");
            }

            ApiInfo info = apiInfoCache["SYNO.FileStation.Upload"];

            // 組合完整的上傳 URL
            var url = $"{baseUrl}/{info.path}?api=SYNO.FileStation.Upload&version={info.maxVersion}&method=upload&_sid={context.Sid}&path={Uri.EscapeDataString(destFolderPath)}&create_parents=true&overwrite=true";

            return url;
        }

        /// <summary>
        /// [FileStation.CreateFolder-create] 建立新資料夾
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<SynoNasCommonResponse> FileStation_CreateFolder(SynoNasConnectionContext context, FileStationCreateFolderRequest model)
        {
            SynoNasCommonResponse res = new SynoNasCommonResponse();
            var @params = new Dictionary<string, string>
            {
                { "folder_path",JsonConvert.SerializeObject(new [] { model.Folder_Path }) },
                { "name", JsonConvert.SerializeObject(new [] { model.Name }) },
                { "force_parent", model.ForceParent ? "true" : "false" },
                { "additional", JsonConvert.SerializeObject(model.Additional) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.FileStation.CreateFolder", "create", @params);
            using var doc = JsonDocument.Parse(content);
            bool success = doc.RootElement.GetProperty("success").GetBoolean();
            if (success)
            {
                res.Success = true;
            }
            else
            {
                res.Success = false;
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                //"{\"error\":{\"code\":1100,\"errors\":[{\"code\":414,\"path\":\"/home/A123\"}]},\"success\":false}"
                if (errCode == "1100")
                {
                    string detailedErrors = doc.RootElement.GetProperty("error").GetProperty("errors")[0].GetProperty("code").GetRawText();
                    res.CommonErrorCode = detailedErrors;
                }
                res.Error = $"{errCode}";
            }
            return res;
        }


        /// <summary>
        /// [FileStation.Rename - rename] 重新命名檔案或資料夾
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model">重新命名請求參數</param>
        /// <returns></returns>
        public async Task<SynoNasCommonResponse> FileStation_Rename(SynoNasConnectionContext context, FileStationRenameRequest model)
        {
            SynoNasCommonResponse res = new SynoNasCommonResponse();
            var @params = new Dictionary<string, string>
            {
                { "path", JsonConvert.SerializeObject(model.Path) },
                { "name", JsonConvert.SerializeObject(model.Name) },
                { "additional", JsonConvert.SerializeObject(model.Additional) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.FileStation.Rename", "rename", @params);
            using var doc = JsonDocument.Parse(content);
            bool success = doc.RootElement.GetProperty("success").GetBoolean();
            if (success)
            {
                res.Success = true;
            }
            else
            {
                res.Success = false;
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                if (errCode == "1100")
                {
                    string detailedErrors = doc.RootElement.GetProperty("error").GetProperty("errors")[0].GetProperty("code").GetRawText();
                    res.CommonErrorCode = detailedErrors;
                }
                else
                {
                    res.CommonErrorCode = errCode;
                }
                res.Error = $"{errCode}";
            }
            return res;
        }
    }
    //使用者
    public partial class SynoNasHelper
    {
        /// <summary>
        /// [SYNO.Core.User-list] 列出所有使用者
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<CoreUserListResponse> CoreUserList(SynoNasConnectionContext context, CoreUserListRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "offset", model.Offset.ToString() },
                { "limit", model.Limit.ToString() },
                {"additional", JsonConvert.SerializeObject(model.Additional)  }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.User", "list", @params);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                var data = doc.RootElement.GetProperty("data");
                string jsonString = data.ToString();
                return JsonConvert.DeserializeObject<CoreUserListResponse>(jsonString);
            }
            else
            {
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                throw new Exception($"{errCode}");
            }
        }
        /// <summary>
        /// [SYNO.Core.User.PasswordConfirm - auth] 驗證使用者密碼
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="password">使用者密碼</param>
        /// <returns></returns>
        public async Task<bool> CoreUserPasswordConfirm(SynoNasConnectionContext context, string password)
        {
            var @params = new Dictionary<string, string>
            {
                { "password", password }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.User.PasswordConfirm", "auth", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }
    }


    //群組
    public partial class SynoNasHelper
    {
        /// <summary>
        /// [SYNO.Core.Group-list] 列出所有群組
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<CoreGroupListResponse> CoreGroupList(SynoNasConnectionContext context, CoreGroupListRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "offset", model.Offset.ToString() },
                { "limit", model.Limit.ToString() },
                { "name_only", model.Name_Only ? "true" : "false" },
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Group", "list", @params);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                var data = doc.RootElement.GetProperty("data");
                string jsonString = data.ToString();
                return JsonConvert.DeserializeObject<CoreGroupListResponse>(jsonString);
            }
            else
            {
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                throw new Exception($"{errCode}");
            }
        }

        /// <summary>
        /// 新增群組
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public async Task<bool> CoreGroupCreate(SynoNasConnectionContext context, string name, string description)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", name },
                { "description", description },
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Group", "create", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// 群組update description
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public async Task<bool> CoreGroupSet(SynoNasConnectionContext context, string name, string description)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", name },
                { "description", description },
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Group", "set", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// 刪除群組
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="names"></param>
        /// <returns></returns>
        public async Task<bool> CoreGroupDelete(SynoNasConnectionContext context, List<string> names)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", JsonConvert.SerializeObject(names) },
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Group", "delete", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// 列出指定群組的分享資料夾權限
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<CoreShareGroupPermissionListResponse> CoreShareGroupPermissionList(SynoNasConnectionContext context, CoreShareGroupPermissionListRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", model.Name },
                { "user_group_type", model.User_Group_Type },
                { "share_type", JsonConvert.SerializeObject(model.Share_Type) },
                { "additional", JsonConvert.SerializeObject(model.Additional) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Share.Permission", "list_by_group", @params);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                var data = doc.RootElement.GetProperty("data");
                string jsonString = data.ToString();
                return JsonConvert.DeserializeObject<CoreShareGroupPermissionListResponse>(jsonString);
            }
            else
            {
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                throw new Exception($"{errCode}");
            }
        }

        /// <summary>
        /// [SYNO.Core.Share.Permission - set_by_user_group] 設定群組的分享資料夾權限
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> CoreShareGroupPermissionSet(SynoNasConnectionContext context, CoreShareGroupPermissionSetRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", model.Name },
                { "user_group_type", model.User_Group_Type },
                { "permissions", JsonConvert.SerializeObject(model.Permissions) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Share.Permission", "set_by_user_group", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// [SYNO.Core.Group.Member - change] 變更群組成員
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> CoreGroupMemberChange(SynoNasConnectionContext context, CoreGroupMemberChangeRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "group", model.Group },
                { "add_member", JsonConvert.SerializeObject(model.Add_Member) },
                { "remove_member", JsonConvert.SerializeObject(model.Remove_Member) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Group.Member", "change", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }


    }

    //共享資料夾
    public partial class SynoNasHelper
    {
        /// <summary>
        /// [SYNO.Core.Share - create] 新增共享資料夾
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> CoreShareFolderCreate(SynoNasConnectionContext context, CoreShareFolderCreateRequest model)
        {
            var shareInfo = new
            {
                name = model.Name,
                vol_path = "/volume1",
                desc = model.Desc,
                enable_share_cow = model.Enable_Share_Cow,
                enable_share_compress = model.Enable_Share_Compress,
                name_org = model.Name_Org
            };

            var @params = new Dictionary<string, string>
            {
                { "name", model.Name },
                { "shareinfo", JsonConvert.SerializeObject(shareInfo) }
            };

            var content = await ExecutePostAsync(context, "SYNO.Core.Share", "create", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// [SYNO.Core.Share - delete] 刪除共享資料夾
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="names">要刪除的共享資料夾名稱列表</param>
        /// <returns></returns>
        public async Task<bool> CoreShareFolderDelete(SynoNasConnectionContext context, List<string> names)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", JsonConvert.SerializeObject(names) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Share", "delete", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// [SYNO.Core.Share - set] 更新共享資料夾設定
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> CoreShareFolderSet(SynoNasConnectionContext context, CoreShareFolderSetRequest model)
        {
            var shareInfo = new
            {
                name = model.Name,
                vol_path = "/volume1",
                desc = model.Desc,
                enable_share_cow = model.Enable_Share_Cow,
                enable_share_compress = model.Enable_Share_Compress,
                encryption = model.Encryption,
                enc_passwd = model.Enc_Passwd
            };

            var @params = new Dictionary<string, string>
            {
                { "name", model.Name_Org },
                { "shareinfo", JsonConvert.SerializeObject(shareInfo) }
            };

            var content = await ExecutePostAsync(context, "SYNO.Core.Share", "set", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// [SYNO.Core.Share.Permission - list] 列出指定共享資料夾的權限
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model">請求參數</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<CoreSharePermissionListByShareResponse> CoreShareFolderPermissionUserList(SynoNasConnectionContext context, CoreShareFolderPermissionListRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "action", "enum"},
                { "offset", "0" },
                { "limit", "-1"},
                { "is_unite_permission",  "false" },
                { "with_inherit", "true" },
                { "name", model.Name },
                { "user_group_type", "local_user" }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Share.Permission", "list", @params);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                var data = doc.RootElement.GetProperty("data");
                string jsonString = data.ToString();
                return JsonConvert.DeserializeObject<CoreSharePermissionListByShareResponse>(jsonString);
            }
            else
            {
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                throw new Exception($"{errCode}");
            }
        }

        /// <summary>
        /// [SYNO.Core.Share.Permission - list] 列出指定共享資料夾的群組權限
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<CoreSharePermissionListByShareResponse> CoreShareFolderPermissionGroupList(SynoNasConnectionContext context, CoreShareFolderPermissionListRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "action", "enum"},
                { "offset", "0" },
                { "limit", "-1"},
                { "is_unite_permission",  "false" },
                { "with_inherit", "true" },
                { "name", model.Name },
                { "user_group_type", "local_group" }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Share.Permission", "list", @params);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.GetProperty("success").GetBoolean())
            {
                var data = doc.RootElement.GetProperty("data");
                string jsonString = data.ToString();
                return JsonConvert.DeserializeObject<CoreSharePermissionListByShareResponse>(jsonString);
            }
            else
            {
                string errCode = doc.RootElement.GetProperty("error").GetProperty("code").GetRawText();
                throw new Exception($"{errCode}");
            }
        }

        /// <summary>
        /// [SYNO.Core.Share.Permission - set] 設定共享資料夾的使用者權限
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model">請求參數</param>
        /// <returns></returns>
        public async Task<bool> CoreShareFolderPermissionSetByUser(SynoNasConnectionContext context, CoreShareFolderPermissionSetRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", model.Name },
                { "user_group_type", "local_user"},
                { "permissions", JsonConvert.SerializeObject(model.Permissions) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Share.Permission", "set", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// [SYNO.Core.Share.Permission - set] 設定共享資料夾的群組權限
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> CoreShareFolderPermissionSetByGroup(SynoNasConnectionContext context, CoreShareFolderPermissionSetRequest model)
        {
            var @params = new Dictionary<string, string>
            {
                { "name", model.Name },
                { "user_group_type", "local_group"},
                { "permissions", JsonConvert.SerializeObject(model.Permissions) }
            };
            var content = await ExecuteGetAsync(context, "SYNO.Core.Share.Permission", "set", @params);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }
    }
    #region 檔案類別
    public class FileItem
    {
        public string name { get; set; }
        public string path { get; set; }
        public bool isdir { get; set; }

        public FileAdditional additional { get; set; }

        public FileStationListResponse children { get; set; }

        public FileStationDirSizeResponse? FolderInfo { get; set; }

    }

    public class FileAdditional
    {
        public long size { get; set; }             // 檔案大小 (Bytes)
        public FileTime time { get; set; }         // 時間資訊
        public FileOwner owner { get; set; }       // 擁有者資訊
        public FilePermission perm { get; set; }   // 權限資訊
    }

    public class FileTime
    {
        public long mtime { get; set; } // 最後修改時間 (Unix Timestamp)
        public long atime { get; set; } // 最後存取時間
        public long ctime { get; set; } // 建立時間
        public long crtime { get; set; } // 屬性改變時間
    }
    public class FileOwner
    {
        public string user { get; set; }   // 擁有者的使用者名稱 (例如: admin)
        public string group { get; set; }  // 擁有者的群組名稱 (例如: users)
        public int uid { get; set; }       // Linux 使用者 ID
        public int gid { get; set; }       // Linux 群組 ID
    }

    public class FilePermission
    {
        // 傳統 Linux 權限數值 (例如: 777, 644)
        public int posix { get; set; }

        // ACL 權限模式 (只有在 NAS 開啟 ACL 支援時才會有值)
        public FileAcl acl { get; set; }

        // 目前登入的使用者對此檔案的權限組合 (ACL 旗標)
        // 這一串數字通常需要透過 位元運算 (Bitwise) 來解析
        public bool is_acl_mode { get; set; }
    }

    public class FileAcl
    {
        public bool append { get; set; }
        public bool control { get; set; }
        public bool del { get; set; }
        public bool exec { get; set; }
        public bool read { get; set; }
        public bool write { get; set; }
    }
    #endregion

    #region Req
    public class FileStationShareListRequest()
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public string SortBy { get; set; } = "name"; // 排序欄位
        public string SortDirection { get; set; } = "asc"; // 升冪或降冪

        public bool OnlyWritable { get; set; } = true; // 是否僅列出可寫入的分享資料夾

        public List<string> Additional { get; set; } = new List<string> { "real_path", "owner", "time", "perm", "mount_point_type", "sync_share", "volume_status" };
    }
    public class FileStationListRequest()
    {
        /// <summary>
        /// 目錄路徑
        /// </summary>
        public string FolderPath { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public string SortBy { get; set; } = "name"; // 排序欄位
        public string SortDirection { get; set; } = "asc"; // 升冪或降冪

        /// <summary>
        /// 檔案類型篩選，可選值包括 "all"（所有類型）、"file"（僅檔案）、"dir"（僅資料夾）
        /// </summary>
        public string Filetype { get; set; } = "all"; // 檔案類型篩選

        public List<string> Additional { get; set; } = new List<string> { "real_path", "size", "owner", "time", "perm", "type" };
    }

    public class FileStationDownloadRequest
    {
        public string path { get; set; }

        public string mode { get; set; } = "download";
    }

    public class FileStationDeleteRequest
    {
        public List<string> path { get; set; }
        public bool recursive { get; set; } = true;
    }

    public class FileStationUploadRequest
    {
        /// <summary>
        /// 目標資料夾路徑
        /// </summary>
        public string DestFolderPath { get; set; }

        /// <summary>
        /// 檔案名稱
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 檔案內容 (使用 Stream 以節省記憶體)
        /// </summary>
        public Stream FileContent { get; set; }

        /// <summary>
        /// 檔案 MIME 類型
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 如果父資料夾不存在，是否自動建立
        /// </summary>
        public bool CreateParents { get; set; } = true;

        /// <summary>
        /// 如果檔案已存在，是否覆寫
        /// </summary>
        public bool Overwrite { get; set; } = true;
    }

    public class FileStationCreateFolderRequest
    {
        public string Folder_Path { get; set; }
        public string Name { get; set; }
        public bool ForceParent { get; set; } = false;

        public List<string> Additional { get; set; } = new List<string> { "real_path", "size", "owner", "time", "perm", "type" };
    }

    public class FileStationRenameRequest
    {
        /// <summary>
        /// 檔案或資料夾的完整路徑
        /// </summary>
        public List<string> Path { get; set; }

        /// <summary>
        /// 新的名稱（不含路徑）
        /// </summary>
        public List<string> Name { get; set; }

        /// <summary>
        /// 額外資訊（可選）
        /// </summary>
        public List<string> Additional { get; set; } = new List<string> { "real_path", "size", "owner", "time", "perm", "type" };

        /// <summary>
        /// 搜尋子目錄（預設: false）
        /// </summary>
        public bool SearchTaskId { get; set; } = false;
    }

    public class CoreShareGroupPermissionListRequest
    {
        public string Name { get; set; }

        public string User_Group_Type { get; set; } = "local_group";


        //["dec", "local", "usb", "sata", "cluster", "c2", "cold_storage", "worm"]
        public List<string> Share_Type { get; set; } = new List<string> { "dec", "local", "usb", "sata", "cluster", "c2", "cold_storage", "worm" };

        //["hidden","encryption","is_aclmode"]
        public List<string> Additional { get; set; } = new List<string> { "real_path", "owner", "time", "perm", "mount_point_type", "sync_share", "volume_status" };
    }

    public class CoreShareFolderPermissionListRequest
    {
        /// <summary>
        /// 操作類型 (預設: enum)
        /// </summary>
        //public string Action { get; set; } = "enum";

        /// <summary>
        /// 偏移量
        /// </summary>
        //public int Offset { get; set; } = 0;

        /// <summary>
        /// 限制數量
        /// </summary>
        //public int Limit { get; set; } = -1;

        /// <summary>
        /// 是否統一權限
        /// </summary>
        //public bool Is_Unite_Permission { get; set; } = false;


        /// <summary>
        /// 共享資料夾名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 使用者群組類型 (預設: local_user)
        /// </summary>
        //public string User_Group_Type { get; set; } = "local_user";
    }

    public class CoreShareFolderPermissionSetRequest
    {
        /// <summary>
        /// 共享資料夾名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 使用者群組類型 (預設: local_user)
        /// </summary>
        //public string User_Group_Type { get; set; } = "local_user";

        /// <summary>
        /// 使用者權限設定列表
        /// </summary>
        public List<PermissionItem> Permissions { get; set; } = new List<PermissionItem>();
    }
    #endregion

    public class FileStationDirSizeResponse
    {
        public bool finished { get; set; }
        public int num_dir { get; set; }
        public int num_file { get; set; }

        public int total_size { get; set; }
    }

    public class FileStationListResponse
    {
        public int total { get; set; }
        public int offset { get; set; }
        public List<FileItem> files { get; set; }
    }
    public class FileStationShareListResponse
    {
        public int total { get; set; }
        public int offset { get; set; }
        public List<FileItem> shares { get; set; }
    }

    public class CoreShareGroupPermissionListResponse
    {
        public int total { get; set; }
        public List<CoreSharePermissionItem> shares { get; set; }
    }

    public class CoreSharePermissionListByShareResponse
    {
        public int total { get; set; }
        public List<SharePermissionUserItem> items { get; set; }
    }

    public class SharePermissionUserItem
    {
        public string inherit { get; set; }
        public bool is_admin { get; set; }
        public bool is_custom { get; set; }
        public bool is_deny { get; set; }
        public bool is_readonly { get; set; }
        public bool is_writable { get; set; }
        public string name { get; set; }
    }

    public class CoreSharePermissionItem
    {
        public bool is_aclmode { get; set; }
        public bool is_custom { get; set; }
        public bool is_deny { get; set; }
        public bool is_mask { get; set; }
        public bool is_readonly { get; set; }
        public bool is_sync_share { get; set; }

        public bool is_unite_permission { get; set; }
        public bool is_writable { get; set; }
        public string name { get; set; }
        public string share_path { get; set; }

    }

    #region 群組相關

    public class CoreGroupListRequest
    {
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = -1;

        public bool Name_Only { get; set; } = false;
    }

    public class CoreGroupListResponse
    {
        public int total { get; set; }
        public int offset { get; set; }
        public List<GroupItem> groups { get; set; }
    }

    public class GroupItem
    {
        public string name { get; set; }
        public int gid { get; set; }
        public string description { get; set; }
    }

    public class CoreShareGroupPermissionSetRequest
    {
        /// <summary>
        /// 群組名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 使用者群組類型 (預設: local_group)
        /// </summary>
        public string User_Group_Type { get; set; } = "local_group";

        /// <summary>
        /// 權限設定列表
        /// </summary>
        public List<PermissionItem> Permissions { get; set; } = new List<PermissionItem>();
    }

    public class PermissionItem
    {
        /// <summary>
        /// 分享資料夾名稱
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 是否為唯讀
        /// </summary>
        public bool is_readonly { get; set; }

        /// <summary>
        /// 是否可寫入
        /// </summary>
        public bool is_writable { get; set; }

        /// <summary>
        /// 是否拒絕存取
        /// </summary>
        public bool is_deny { get; set; }

        /// <summary>
        /// 是否為自訂權限
        /// </summary>
        public bool is_custom { get; set; } = false;
    }

    public class CoreGroupMemberChangeRequest
    {
        /// <summary>
        /// 群組名稱
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 要新增的成員列表
        /// </summary>
        public List<string> Add_Member { get; set; } = new List<string>();

        /// <summary>
        /// 要移除的成員列表
        /// </summary>
        public List<string> Remove_Member { get; set; } = new List<string>();
    }

    public class CoreShareFolderCreateRequest
    {
        /// <summary>
        /// 共享資料夾名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 儲存空間路徑 (例如: /volume1)
        /// </summary>
        //public string Vol_Path { get; set; } = "/volume1";

        /// <summary>
        /// 共享資料夾描述
        /// </summary>
        public string Desc { get; set; } = "";

        /// <summary>
        /// 是否啟用 Btrfs 共享資料夾快照功能
        /// </summary>
        public bool Enable_Share_Cow { get; set; } = false;

        /// <summary>
        /// 是否啟用 Btrfs 共享資料夾壓縮功能
        /// </summary>
        public bool Enable_Share_Compress { get; set; } = false;

        /// <summary>
        /// 原始名稱 (選填)
        /// </summary>
        public string Name_Org { get; set; } = "";
    }

    public class CoreShareFolderSetRequest
    {
        /// <summary>
        /// 共享資料夾名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 共享資料夾描述
        /// </summary>
        public string Desc { get; set; } = "";

        /// <summary>
        /// 是否啟用 Btrfs 共享資料夾快照功能
        /// </summary>
        public bool Enable_Share_Cow { get; set; } = false;

        /// <summary>
        /// 是否啟用 Btrfs 共享資料夾壓縮功能
        /// </summary>
        public bool Enable_Share_Compress { get; set; } = false;

        /// <summary>
        /// 是否啟用加密
        /// </summary>
        public bool Encryption { get; set; } = false;

        /// <summary>
        /// 加密密碼
        /// </summary>
        public string Enc_Passwd { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        public string Name_Org { get; set; } = "";
    }

    #endregion

    #region 使用者相關

    public class CoreUserListRequest
    {
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = -1;

        /// <summary>
        /// ["email","description","expired","2fa_status"]
        /// </summary>
        public List<string> Additional { get; set; } = new List<string> { "email", "description", "expired", "2fa_status" };
    }

    public class CoreUserListResponse
    {
        public int total { get; set; }
        public int offset { get; set; }
        public List<UserItem> users { get; set; }
    }

    public class UserItem
    {
        public string name { get; set; }
        public string expired { get; set; }
        public string description { get; set; }
        public string email { get; set; }

        //public bool 2fa_status { get; set; }

    }

    #endregion


    public class SynoNasCommonResponse<T>
    {
        public bool Success { get; set; }
        public string CommonErrorCode { get; set; }

        public string Error { get; set; }
        public T Data { get; set; }
    }

    public class SynoNasCommonResponse
    {
        public bool Success { get; set; }

        public string CommonErrorCode { get; set; }
        public string Error { get; set; }

    }

    /// <summary>
    /// 資料夾樹狀節點
    /// </summary>
    public partial class SynoNasHelper
    {
        /// <summary>
        /// 公司資料夾，取出全部節點用 (邏輯再確認)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<List<NasTreeNodeDto>> GetRootNodesAsync(SynoNasConnectionContext context)
        {
            var shares = await ListSharesAsync(context);

            return shares
                .Where(x => x.IsDir)
                .Select(x => MapSimpleNode(x))
                .ToList();
        }

        public async Task<NasTreeNodeDto> GetRootNodeAsync(SynoNasConnectionContext context, string path)
        {
            // 根節點顯示名稱：用路徑最後一段；你也可以改成固定文字例如「部門資料夾」
            var rootName = GetLastSegment(path);

            // 不要在 root 就把 children 全載入，維持 lazy load
            return new NasTreeNodeDto
            {
                Id = context.RootFolderPath,
                Name = rootName,
                Path = context.RootFolderPath,
                IsDir = true,
                HasChildren = true,
                Loaded = false,
                Expanded = false,
                Children = new List<NasTreeNodeDto>()
            };
        }

        private static string GetLastSegment(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";
            var p = path.TrimEnd('/');
            var idx = p.LastIndexOf('/');
            return idx >= 0 ? p[(idx + 1)..] : p;
        }

        public async Task<List<NasTreeNodeDto>> GetChildrenAsync(SynoNasConnectionContext context, string path)
        {
            var folders = await ListDirectoriesAsync(context, path);

            return folders
                .Where(x => x.IsDir)
                .Select(x => MapSimpleNode(x))
                .ToList();
        }

        private NasTreeNodeDto MapSimpleNode(SynologyFileItem item)
        {
            return new NasTreeNodeDto
            {
                Id = item.Path,
                Name = item.Name,
                Path = item.Path,
                IsDir = item.IsDir,
                HasChildren = true,   // 第一次先假設可展開；真正點開若為空再修正
                Loaded = false,
                Expanded = false,
                Children = new List<NasTreeNodeDto>()
            };
        }

        private NasTreeNodeDto MapExpandedNode(SynologyFileItem item, string targetPath)
        {
            var childItems = item.Children?.Files?
                .Where(x => x.IsDir)
                .ToList() ?? new List<SynologyFileItem>();

            var childNodes = childItems
                .Select(x => MapExpandedNode(x, targetPath))
                .ToList();

            return new NasTreeNodeDto
            {
                Id = item.Path,
                Name = item.Name,
                Path = item.Path,
                IsDir = item.IsDir,
                HasChildren = childNodes.Any() || IsSameOrAncestorPath(item.Path, targetPath),
                Loaded = childNodes.Any(),
                Expanded = childNodes.Any(),
                Children = childNodes
            };
        }

        private bool IsSameOrAncestorPath(string currentPath, string targetPath)
        {
            if (string.Equals(
                    currentPath.TrimEnd('/'),
                    targetPath.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var current = currentPath.TrimEnd('/');
            var target = targetPath.TrimEnd('/');

            return target.StartsWith(current + "/", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 樹狀節點 底層方法
    /// </summary>
    public partial class SynoNasHelper
    {
        public async Task<List<SynologyFileItem>> ListSharesAsync(SynoNasConnectionContext context)
        {

            var content = await ExecuteGetAsync(context, "SYNO.FileStation.List", "list_share");

            var result = System.Text.Json.JsonSerializer.Deserialize<SynologyListShareResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });


            return result.Data?.Shares ?? new List<SynologyFileItem>();
        }

        public async Task<List<SynologyFileItem>> ListDirectoriesAsync(SynoNasConnectionContext context, string folderPath)
        {
            var @params = new Dictionary<string, string>
            {
                { "folder_path", folderPath }
            };
            var content = await ExecuteGetAsync(context, "SYNO.FileStation.List", "list", @params);

            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (!doc.RootElement.GetProperty("success").GetBoolean())
            {
                int code = doc.RootElement.GetProperty("error").GetProperty("code").GetInt32();
                throw new SynoNasApiException(code, GetSynologyErrorMessage(code));
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<SynologyListResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result.Data?.Files ?? new List<SynologyFileItem>();
        }

        private Exception CreateSynologyException(int? code, string message)
        {
            return new Exception($"{message} {GetSynologyErrorMessage(code)}");
        }

        private string GetSynologyErrorMessage(int? code)
        {
            return code switch
            {
                400 => "(帳號不存在或密碼錯誤)",
                401 => "(帳號停用)",
                402 => "(權限不足)",
                403 => "(需要二步驟驗證碼)",
                404 => "(二步驟驗證失敗)",
                105 => "(權限不足)",
                106 => "(Session timeout)",
                107 => "(Session interrupted by duplicate login)",
                119 => "(SID not found)",
                408 => "(找不到檔案或資料夾)",
                418 => "(非法路徑或名稱)",
                _ => code.HasValue ? $"(錯誤碼: {code})" : string.Empty
            };
        }
    }

    /// <summary>
    /// DirSize 查詢
    /// </summary>
    public partial class SynoNasHelper
    {
        public record SynoApiError(int code, JsonElement? errors);
        public record SynoApiResult<T>(bool success, T? data, SynoApiError? error);

        public record DirSizeStartBackUpData(string taskid);
        public record DirSizeStatusData(bool finished, long num_dir, long num_file, long total_size);
        public async Task<DirSizeStartBackUpData> StartAsync(SynoNasConnectionContext context, string folderPath)
        {
            // 文件：path 是「一個或多個路徑」，用 brackets 包起來 :contentReference[oaicite:6]{index=6}
            var pathArray = $"[\"{folderPath}\"]";

            var @params = new Dictionary<string, string>
        {
            { "path", pathArray }
        };

            var json = await ExecuteGetAsync(context, "SYNO.FileStation.DirSize", "start", @params);

            using var doc = JsonDocument.Parse(json);
            EnsureSuccessOrThrow(doc);

            var taskId = doc.RootElement.GetProperty("data").GetProperty("taskid").GetString();
            if (string.IsNullOrWhiteSpace(taskId))
                throw new SynoNasMsgException("Nas_DirSize_TaskId_Missing");

            return new DirSizeStartBackUpData(taskId);
        }

        public async Task<DirSizeStatusData> StatusAsync(SynoNasConnectionContext context, string taskid)
        {
            var @params = new Dictionary<string, string>
        {
            { "taskid", taskid }
        };

            var json = await ExecuteGetAsync(context, "SYNO.FileStation.DirSize", "status", @params);

            using var doc = JsonDocument.Parse(json);
            EnsureSuccessOrThrow(doc);

            var data = doc.RootElement.GetProperty("data");

            // 文件：finished/num_dir/num_file/total_size :contentReference[oaicite:7]{index=7}
            return new DirSizeStatusData(
                finished: data.GetProperty("finished").GetBoolean(),
                num_dir: data.GetProperty("num_dir").GetInt64(),
                num_file: data.GetProperty("num_file").GetInt64(),
                total_size: data.GetProperty("total_size").GetInt64()
            );
        }

        public async Task StopAsync(SynoNasConnectionContext context, string taskid)
        {
            var @params = new Dictionary<string, string>
        {
            { "taskid", taskid }
        };

            var json = await ExecuteGetAsync(context, "SYNO.FileStation.DirSize", "stop", @params);

            using var doc = JsonDocument.Parse(json);
            EnsureSuccessOrThrow(doc);
            // stop 成功通常 data 為空 :contentReference[oaicite:8]{index=8}
        }

        private static void EnsureSuccessOrThrow(JsonDocument doc)
        {
            if (doc.RootElement.TryGetProperty("success", out var successEl) && successEl.GetBoolean())
                return;

            var code = doc.RootElement.GetProperty("error").GetProperty("code").GetInt32();
            throw new SynoApiException(code, doc.RootElement.GetProperty("error").ToString());
        }


    }

    public class SynoApiException : Exception
    {
        public int Code { get; }
        public SynoApiException(int code, string message) : base(message) => Code = code;
    }

    /// <summary>
    /// CopyMove 任務
    /// </summary>
    public partial class SynoNasHelper
    {
        public record CopyMoveStartData(string taskid);
        public record CopyMoveStatusData(bool finished, double progress);

        /// <summary>
        /// [SYNO.FileStation.CopyMove - start] 啟動複製/移動任務
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="paths">來源路徑清單</param>
        /// <param name="destFolderPath">目標資料夾</param>
        /// <param name="removeSrc">true=移動，false=複製</param>
        public async Task<CopyMoveStartData> CopyMove_StartAsync(SynoNasConnectionContext context, List<string> paths, string destFolderPath, bool removeSrc = true)
        {
            var @params = new Dictionary<string, string>
            {
                { "path", JsonConvert.SerializeObject(paths) },
                { "dest_folder_path", destFolderPath },
                { "remove_src", removeSrc ? "true" : "false" }
            };

            var json = await ExecuteGetAsync(context, "SYNO.FileStation.CopyMove", "start", @params);

            using var doc = JsonDocument.Parse(json);
            EnsureSuccessOrThrow(doc);

            var taskId = doc.RootElement.GetProperty("data").GetProperty("taskid").GetString();
            if (string.IsNullOrWhiteSpace(taskId))
                throw new SynoNasMsgException("Nas_CopyMove_TaskId_Missing");

            return new CopyMoveStartData(taskId);
        }

        /// <summary>
        /// [SYNO.FileStation.CopyMove - status] 查詢任務狀態
        /// </summary>
        /// <param name="context">連線資訊</param>
        /// <param name="taskid">任務 ID</param>
        public async Task<CopyMoveStatusData> CopyMove_StatusAsync(SynoNasConnectionContext context, string taskid)
        {
            var @params = new Dictionary<string, string>
            {
                { "taskid", taskid }
            };

            var json = await ExecuteGetAsync(context, "SYNO.FileStation.CopyMove", "status", @params);

            using var doc = JsonDocument.Parse(json);
            EnsureSuccessOrThrow(doc);

            var data = doc.RootElement.GetProperty("data");
            return new CopyMoveStatusData(
                finished: data.GetProperty("finished").GetBoolean(),
                progress: data.TryGetProperty("progress", out var p) ? p.GetDouble() : 0.0
            );
        }
    }
}
