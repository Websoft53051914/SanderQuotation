using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace backend.AI
{
    /// <summary>
    /// 封裝 Gemini File API 的影片上傳邏輯。
    /// 使用 resumable upload 協議：先取得 upload URL，再上傳影片內容。
    /// </summary>
    public class GeminiFileApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BASE_URL = "https://generativelanguage.googleapis.com";
        private const string UploadBaseUrl = $"{BASE_URL}/upload/v1beta/files";
        private const string FilesBaseUrl   = $"{BASE_URL}/v1beta/files";

        public GeminiFileApiClient(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey     = apiKey;
        }

        /// <summary>
        /// 上傳Stream 至 Gemini File API，並等待處理完成後回傳 file URI。
        /// </summary>
        /// <param name="stream">Stream（需可讀取長度）</param>
        /// <param name="mimeType">MIME type，例如 "video/mp4"</param>
        /// <param name="displayName">檔案顯示名稱（選填）</param>
        /// <returns>可供 Gemini Chat 引用的 file URI</returns>
        public async Task<string> UploadFileAsync(Stream stream, string mimeType = "video/mp4", string displayName = "upload_video")
        {
            try
            {   
                // Step 1: 初始化上傳並取得 upload URL
                var fileSize = stream.Length;
                var uploadUrl = await InitializeUploadAsync(displayName, mimeType, fileSize);

                // Step 2: 使用 Stream 上傳檔案內容
                var fileName = await UploadFileContentAsync(uploadUrl, stream, fileSize, mimeType);

                // Step 3: 等待檔案處理完成
                await WaitForFileProcessingAsync(fileName);

                // Step 4: 取得檔案資訊
                var fileInfo = await GetFileInfoAsync(fileName);

                return fileInfo.uri;
            }
            catch (Exception ex)
            {
                throw new Exception($"上傳檔案失敗: {ex.Message}", ex);
            }
        }

      

        /// <summary>
        /// 刪除已上傳的 Gemini 檔案（釋放配額）
        /// </summary>
        public async Task DeleteFileAsync(string fileUri)
        {
            // fileUri 格式: https://generativelanguage.googleapis.com/v1beta/files/{fileId}
            string deleteUrl = $"{fileUri}?key={_apiKey}";
            await _httpClient.DeleteAsync(deleteUrl);
        }

        // ── 私有輔助方法 ──────────────────────────────────────────
        /// <summary>
        /// 初始化上傳會話
        /// </summary>
        private async Task<string> InitializeUploadAsync(string displayName, string mimeType, long fileSize)
        {
            var url = $"{BASE_URL}/upload/v1beta/files?key={_apiKey}";

            var metadata = new
            {
                file = new
                {
                    display_name = displayName
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(metadata),
                Encoding.UTF8,
                "application/json"
            );

            content.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            content.Headers.Add("X-Goog-Upload-Command", "start");
            content.Headers.Add("X-Goog-Upload-Header-Content-Length", fileSize.ToString());
            content.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            if (response.Headers.TryGetValues("X-Goog-Upload-URL", out var values))
            {
                return values.First();
            }

            throw new Exception("無法取得上傳 URL");
        }

        /// <summary>
        /// 使用 Stream 上傳檔案內容
        /// </summary>
        private async Task<string> UploadFileContentAsync(string uploadUrl, Stream stream, long fileSize, string mimeType)
        {
            using var content = new StreamContent(stream);

            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            content.Headers.ContentLength = fileSize;

            var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
            {
                Content = content
            };

            request.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            request.Headers.Add("X-Goog-Upload-Offset", "0");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (result.TryGetProperty("file", out var file) &&
                file.TryGetProperty("name", out var name))
            {
                return name.GetString() ?? throw new Exception("無法取得檔案名稱");
            }

            throw new Exception("上傳回應格式錯誤");
        }

        /// <summary>
        /// 等待檔案處理完成
        /// </summary>
        private async Task WaitForFileProcessingAsync(string fileName, int maxRetries = 30)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var fileInfo = await GetFileInfoAsync(fileName);

                if (fileInfo.state == "ACTIVE")
                {
                    return;
                }
                else if (fileInfo.state == "FAILED")
                {
                    throw new Exception($"檔案處理失敗: {fileName}");
                }

                // 等待 1 秒後重試
                await Task.Delay(1000);
            }

            throw new TimeoutException($"等待檔案處理超時: {fileName}");
        }

        /// <summary>
        /// 取得檔案資訊
        /// </summary>
        private async Task<(string name, string uri, string state)> GetFileInfoAsync(string fileName)
        {
            var url = $"{BASE_URL}/v1beta/{fileName}?key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            var name = result.GetProperty("name").GetString() ?? string.Empty;
            var uri = result.GetProperty("uri").GetString() ?? string.Empty;
            var state = result.GetProperty("state").GetString() ?? string.Empty;

            return (name, uri, state);
        }

    }
}
