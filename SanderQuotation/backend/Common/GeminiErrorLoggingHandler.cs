using backend.AI;

namespace backend.Common
{
    /// <summary>
    /// 攔截 Gemini API HTTP 回應，將 400/5xx 錯誤的實際 Response Body 寫入 Log
    /// </summary>
    public class GeminiErrorLoggingHandler : DelegatingHandler
    {

        public GeminiErrorLoggingHandler()
        {
            
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                AICommon.LogError(
                    new Exception($"Gemini API Error: StatusCode={(int)response.StatusCode}, URL={request.RequestUri}, Body={errorBody}"),
                    new { Url = request.RequestUri, StatusCode = (int)response.StatusCode, Body = errorBody },
                    method: nameof(GeminiErrorLoggingHandler)
                );
            }

            return response;
        }
    }
}