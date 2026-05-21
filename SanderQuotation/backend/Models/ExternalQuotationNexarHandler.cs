/**
 * http://portal.nexar.com/
 * * Nexar API 額度、API 測試程式(Playground)
 * * API 驗證資訊位置： 點擊選單【Apps】→點擊計劃【Evaluation app】→點擊頁籤【Authorization】→【Credentials】區塊
 * https://support.nexar.com/support/solutions/articles/101000494582-nexar-playground-graphql-query-examples
 * * Nexar API 官方文件
 */
using Const;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace backend.Models
{
    /// <summary>
    /// Nexar 外部查價
    /// </summary>
    public partial class ExternalQuotationNexarHandler
    {
        /// <summary>
        /// Cache Key：Nexar Access Token
        /// </summary>
        protected const string CacheKeyOfAccessToken = "NEXAR_ACCESS_TOKEN";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// AccessToken 物件
        /// </summary>
        private GetAccessTokenAsyncResM? _dataAccessToken = null;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="httpClientFactory">HTTP 客戶端工廠</param>
        /// <param name="cache">記憶體快取</param>
        /// <param name="configuration">應用程式設定</param>
        public ExternalQuotationNexarHandler(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _configuration = configuration;
        }
    }

    public partial class ExternalQuotationNexarHandler
    {
        /// <summary>
        /// 取得 Nexar API 存取金鑰（含快取）
        /// </summary>
        /// <exception cref="Exception">取得 Access Token 失敗時拋出例外</exception>
        public async Task Authorization()
        {
            GetAccessTokenAsyncResM? result = null;

            if (_cache.TryGetValue(CacheKeyOfAccessToken, out result))
            {
                if (result != null)
                {
                    _dataAccessToken = result;
                    return;
                }
            }

            ApiTaskResult<GetAccessTokenAsyncResM> ret = await CallGetAccessTokenAsync();
            if (ret.Status == ApiTaskStatusEnum.Success)
            {
                result = ret.Data;
            }
            else
            {
                throw new Exception(ret.Message);
            }

            MemoryCacheEntryOptions cacheOption = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(23)
            };
            _cache.Set(CacheKeyOfAccessToken, result, cacheOption);

            _dataAccessToken = result;
        }

        /// <summary>
        /// 依 MPN 向 Nexar 外部查價，回傳最佳供應商報價
        /// </summary>
        /// <param name="bomVO">BOM 料項資訊（需含廠商型號與需求量）</param>
        /// <param name="configPreferredVendorList">優先供應商名稱清單</param>
        /// <returns>外部查價結果，找不到時回傳空白的 VO</returns>
        public async Task<ExternalQuotationRecordVO> Search(PriceBomVO bomVO, List<string> configPreferredVendorList)
        {
            ArgumentNullException.ThrowIfNull(_dataAccessToken);

            int bomQty = bomVO.Qty ?? 0;
            ExternalQuotationRecordVO result = new();

            ApiTaskResult<NexarPartResult> ret = await CallSupSearchMpnAsync(bomVO.ManufacturerPartNumber ?? string.Empty);

            if (ret.Status == ApiTaskStatusEnum.Success && ret.Data != null)
            {
                NexarData? data = ret.Data.Data;
                if (data != null
                    && data.SupSearchMpn != null
                    && data.SupSearchMpn.Results != null
                    && data.SupSearchMpn.Results.Count > 0
                    && data.SupSearchMpn.Results.First().Part != null
                    && data.SupSearchMpn.Results.First().Part?.Mpn == bomVO.ManufacturerPartNumber)
                {
                    PartDetail? dataPartDetail = data.SupSearchMpn.Results.First().Part;
                    ArgumentNullException.ThrowIfNull(dataPartDetail);

                    List<ExternalQuotationRecordVO> sellerList = dataPartDetail.Sellers?
                        .SelectMany(seller => seller.Offers?
                            .Where(offer =>
                                offer.Moq.HasValue
                                && offer.Moq.Value <= bomQty
                                && offer.InventoryLevel.HasValue
                                && offer.InventoryLevel.Value >= bomQty)
                            .Select(offer =>
                            {
                                PriceBreak? targetPrice = offer.Prices?
                                    .Where(p => p.Quantity.HasValue && p.Quantity.Value <= bomQty)
                                    .OrderByDescending(p => p.Quantity ?? 0)
                                    .FirstOrDefault();

                                if (targetPrice == null) return null;

                                ExternalQuotationRecordVO vo = new();
                                vo.SupplierName = seller.Company?.Name;
                                vo.UnitPriceOriginalCurrency = targetPrice.Price;
                                vo.Currency = targetPrice.Currency;
                                vo.MOQ = targetPrice.Quantity;
                                vo.UnitPriceTWD = targetPrice.ConvertedPrice;
                                vo.Stock = offer.InventoryLevel.HasValue ? (int)offer.InventoryLevel.Value : default(int?);

                                return vo;
                            })
                            .Where(vo => vo != null)
                            ?? new List<ExternalQuotationRecordVO?>())
                        .Cast<ExternalQuotationRecordVO>()
                        .OrderBy(x => x.UnitPriceTWD ?? decimal.MaxValue)
                        .ToList() ?? [];

                    List<ExternalQuotationRecordVO> preferredSellerList = sellerList
                        .Where(seller => configPreferredVendorList.Contains(seller.SupplierName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (preferredSellerList.Count > 0)
                    {
                        result = preferredSellerList.First();
                    }
                    else
                    {
                        ExternalQuotationRecordVO? selected = sellerList.FirstOrDefault();
                        if (selected != null)
                        {
                            result = selected;
                        }
                    }

                    result.SearchMatchCount = sellerList.Count;
                    result.SearchMatchPreferredCount = preferredSellerList.Count;
                    result.IsPreferred = preferredSellerList.Count > 0;
                }
            }

            return result;
        }
    }

    public partial class ExternalQuotationNexarHandler
    {
        /// <summary>
        /// 呼叫 Nexar API 取得 Access Token
        /// </summary>
        /// <returns>API 呼叫結果</returns>
        private async Task<ApiTaskResult<GetAccessTokenAsyncResM>> CallGetAccessTokenAsync()
        {
            try
            {
                using HttpClient httpClient = _httpClientFactory.CreateClient();

                string clientId = _configuration["ExternalQuotation:Nexar:ClientId"] ?? string.Empty;
                string clientSecret = _configuration["ExternalQuotation:Nexar:ClientSecret"] ?? string.Empty;

                string credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
                );

                HttpRequestMessage request = new(
                    HttpMethod.Post,
                    "https://identity.nexar.com/connect/token"
                );
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                FormUrlEncodedContent content = new(
                [
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "supply.domain")
                ]);
                request.Content = content;

                HttpResponseMessage response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                GetAccessTokenAsyncResM? result = await response.Content.ReadFromJsonAsync<GetAccessTokenAsyncResM>();
                ArgumentNullException.ThrowIfNull(result);

                return ApiTaskResult<GetAccessTokenAsyncResM>.Success(result);
            }
            catch (Exception ex)
            {
                return ApiTaskResult<GetAccessTokenAsyncResM>.Error(ex);
            }
        }

        /// <summary>
        /// 呼叫 Nexar API 以廠商料號（MPN）查詢供應商報價資訊
        /// </summary>
        /// <param name="pMpn">廠商型號（MPN）</param>
        /// <returns>Nexar 查詢結果</returns>
        private async Task<ApiTaskResult<NexarPartResult>> CallSupSearchMpnAsync(string pMpn)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_dataAccessToken);

                using HttpClient httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _dataAccessToken.AccessToken);

                var body = new
                {
                    query = @"
query($mpn: String!) {
  supSearchMpn(q: $mpn, country: ""TW"", currency: ""TWD"", limit: 1) {
    results {
      part {
        mpn
        sellers {
          company {
            name
          }
          offers {
            moq
            inventoryLevel
            prices {
              quantity
              price
              currency
              convertedPrice
              convertedCurrency
            }
          }
        }
      }
    }
  }
}
",
                    variables = new
                    {
                        mpn = pMpn
                    }
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(body);

                HttpResponseMessage response = await httpClient.PostAsync(
                    "https://api.nexar.com/graphql",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                NexarPartResult? result = Newtonsoft.Json.JsonConvert.DeserializeObject<NexarPartResult>(responseContent);

                return ApiTaskResult<NexarPartResult>.Success(result);
            }
            catch (Exception ex)
            {
                return ApiTaskResult<NexarPartResult>.Error(ex);
            }
        }
    }

    /// <summary>
    /// Access Token 回傳物件
    /// </summary>
    public class GetAccessTokenAsyncResM
    {
        /// <summary>
        /// 存取金鑰
        /// </summary>
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// 過期秒數
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    /// <summary>
    /// Nexar 查詢結果根節點
    /// </summary>
    public class NexarPartResult
    {
        /// <summary>
        /// 查詢結果資料
        /// </summary>
        [JsonPropertyName("data")]
        public NexarData? Data { get; set; }
    }

    /// <summary>
    /// Nexar 查詢結果資料節點
    /// </summary>
    public class NexarData
    {
        /// <summary>
        /// MPN 搜尋結果
        /// </summary>
        [JsonPropertyName("supSearchMpn")]
        public SupSearchMpn? SupSearchMpn { get; set; }
    }

    /// <summary>
    /// MPN 搜尋結果清單
    /// </summary>
    public class SupSearchMpn
    {
        /// <summary>
        /// 搜尋結果項目清單
        /// </summary>
        [JsonPropertyName("results")]
        public List<PartSearchResult>? Results { get; set; }
    }

    /// <summary>
    /// 單一搜尋結果項目
    /// </summary>
    public class PartSearchResult
    {
        /// <summary>
        /// 料號元件詳細資訊
        /// </summary>
        [JsonPropertyName("part")]
        public PartDetail? Part { get; set; }
    }

    /// <summary>
    /// 料號元件詳細資訊
    /// </summary>
    public class PartDetail
    {
        /// <summary>
        /// 廠商型號
        /// </summary>
        [JsonPropertyName("mpn")]
        public string? Mpn { get; set; }

        /// <summary>
        /// 供應商清單
        /// </summary>
        [JsonPropertyName("sellers")]
        public List<Seller>? Sellers { get; set; }
    }

    /// <summary>
    /// 供應商資訊
    /// </summary>
    public class Seller
    {
        /// <summary>
        /// 供應商公司資訊
        /// </summary>
        [JsonPropertyName("company")]
        public Company? Company { get; set; }

        /// <summary>
        /// 供應商報價清單
        /// </summary>
        [JsonPropertyName("offers")]
        public List<Offer>? Offers { get; set; }
    }

    /// <summary>
    /// 供應商公司資訊
    /// </summary>
    public class Company
    {
        /// <summary>
        /// 公司名稱
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// 供應商報價（含 MOQ 與價格階梯）
    /// </summary>
    public class Offer
    {
        /// <summary>
        /// 最小訂購量（MOQ）
        /// </summary>
        [JsonPropertyName("moq")]
        public int? Moq { get; set; }

        /// <summary>
        /// 即時庫存量
        /// </summary>
        [JsonPropertyName("inventoryLevel")]
        public decimal? InventoryLevel { get; set; }

        /// <summary>
        /// 價格階梯清單
        /// </summary>
        [JsonPropertyName("prices")]
        public List<PriceBreak>? Prices { get; set; }
    }

    /// <summary>
    /// 價格階梯
    /// </summary>
    public class PriceBreak
    {
        /// <summary>
        /// 起始數量
        /// </summary>
        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        /// <summary>
        /// 單價（原幣）
        /// </summary>
        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 幣別（例如 USD）
        /// </summary>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// 轉換後單價（台幣）
        /// </summary>
        [JsonPropertyName("convertedPrice")]
        public decimal? ConvertedPrice { get; set; }

        /// <summary>
        /// 轉換後幣別（例如 TWD）
        /// </summary>
        [JsonPropertyName("convertedCurrency")]
        public string? ConvertedCurrency { get; set; }
    }
}
