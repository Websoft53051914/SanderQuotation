using Business.DomainModel;
using System.Text.Json;

namespace backend.AI
{
    /// <summary>
    /// 
    /// </summary>
    public class ManualBatchEmbedding
    {
        private IConfiguration _config;
        private string ApiKey = "AIzaSyBX15xCj-3zarIBwgINiDM2CV943Mtij4k";
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="config"></param>
        public ManualBatchEmbedding(IConfiguration config)
        {
            _config = config;
            ApiKey = _config["AccountSalt"]?.ToString();
        }

        // 設定模型名稱
        private const string ModelId = "text-embedding-001";
        // API 網址 (注意這裡是 batchEmbedContents)
        private const string Endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:batchEmbedContents";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task FillEmbed(List<TBSanderModuleItemKeywordDM> data)
        {
            var chunks = data.Chunk(100);

            foreach (var batch in chunks)
            {
                List<TBSanderModuleItemKeywordDM> batch2 = batch
                    .Where(x => x.ColumnName == nameof(SanderModuleItemDM.Description) || x.ColumnName == nameof(SanderModuleItemDM.Description2))
                    .ToList();

                if (batch2.Count == 0)
                    continue;

                // 2. 建構符合 Gemini API 要求的 Request 物件
                var requestPayload = new BatchEmbedRequest
                {
                    Requests = batch2
                    .Select(t => new EmbedRequestItem
                    {
                        Content = new Content { Parts = new[] { new Part { Text = t.Keyword ?? string.Empty } } },
                        Model = $"models/{ModelId}",
                        TaskType = "RETRIEVAL_DOCUMENT"
                    }).ToList()
                };

                // 3. 發送 HTTP 請求
                using var httpClient = new HttpClient();

                try
                {
                    // 加入 API Key 到 URL 參數
                    var urlWithKey = $"{Endpoint}?key={ApiKey}";

                    // 設定 JSON 選項 (轉成小駝峰 camelCase)
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };


                    // PostAsJsonAsync 是 .NET 5+ 的擴充方法
                    var response = await httpClient.PostAsJsonAsync(urlWithKey, requestPayload, jsonOptions);

                    // 4. 處理回應
                    if (response.IsSuccessStatusCode)
                    {
                        // 讀取並解析 JSON
                        var result = await response.Content.ReadFromJsonAsync<BatchEmbedResponse>(jsonOptions);

                        if (result?.Embeddings != null)
                        {
                            for (var i = 0; i < batch2.Count(); i++)
                            {
                                var vector = result.Embeddings[i].Values;
                                // 將向量存回對應的 FAQ 實體
                                batch2.ElementAt(i).KeywordEmbedding = vector;
                            }
                        }
                    }
                    else
                    {
                        // 錯誤處理
                        var errorBody = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API Error: {response.StatusCode}, Body: {errorBody}");
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        public async Task FillEmbed(List<HistoryFileDM> data)
        {
            var chunks = data.Chunk(100);

            foreach (var batch in chunks)
            {
                // 2. 建構符合 Gemini API 要求的 Request 物件
                var requestPayload = new BatchEmbedRequest
                {
                    Requests = batch.Select(t => new EmbedRequestItem
                    {
                        Content = new Content { Parts = new[] { new Part { Text = $"[檔名：{t.FileName}] {t.FileSummary}" } } },
                        Model = $"models/{ModelId}",
                        TaskType = "RETRIEVAL_DOCUMENT"
                    }).ToList()
                };

                // 3. 發送 HTTP 請求
                using var httpClient = new HttpClient();

                try
                {
                    // 加入 API Key 到 URL 參數
                    var urlWithKey = $"{Endpoint}?key={ApiKey}";

                    // 設定 JSON 選項 (轉成小駝峰 camelCase)
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };


                    // PostAsJsonAsync 是 .NET 5+ 的擴充方法
                    var response = await httpClient.PostAsJsonAsync(urlWithKey, requestPayload, jsonOptions);

                    // 4. 處理回應
                    if (response.IsSuccessStatusCode)
                    {
                        // 讀取並解析 JSON
                        var result = await response.Content.ReadFromJsonAsync<BatchEmbedResponse>(jsonOptions);

                        if (result?.Embeddings != null)
                        {
                            for (var i = 0; i < batch.Count(); i++)
                            {
                                var vector = result.Embeddings[i].Values;
                                // 將向量存回對應的 FAQ 實體
                                batch.ElementAt(i).Embedding = vector;
                            }
                        }
                    }
                    else
                    {
                        // 錯誤處理
                        var errorBody = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API Error: {response.StatusCode}, Body: {errorBody}");
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// 根請求物件
    /// </summary>
    public class BatchEmbedRequest
    {
        /// <summary>
        /// 
        /// </summary>
        public List<EmbedRequestItem> Requests { get; set; } = new();
    }

    /// <summary>
    /// 單一請求項目
    /// </summary>
    public class EmbedRequestItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string? Model { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Content Content { get; set; } = new();
        /// <summary>
        /// 選填: RETRIEVAL_QUERY, RETRIEVAL_DOCUMENT 等
        /// </summary>
        public string? TaskType { get; set; }
        /// <summary>
        /// 選填: 僅在 RETRIEVAL_DOCUMENT 時有用
        /// </summary>
        public string? Title { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Content
    {
        /// <summary>
        /// 
        /// </summary>
        public Part[] Parts { get; set; } = [];
    }

    /// <summary>
    /// 
    /// </summary>
    public class Part
    {
        /// <summary>
        /// 
        /// </summary>
        public string? Text { get; set; }
    }

    /// <summary>
    /// 回應物件結構
    /// </summary>
    public class BatchEmbedResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public List<EmbeddingResult> Embeddings { get; set; } = [];
    }
    /// <summary>
    /// 
    /// </summary>
    public class EmbeddingResult
    {
        public float[] Values { get; set; } = [];
    }
}
