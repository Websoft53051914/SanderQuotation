using Const.ApiModels.QueryPrice;
using Const.ApiModels.QueryPrice;
using Core.Utility.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Newtonsoft.Json;
using System.Net.Http.Json;
using static Const.Enums;

namespace backend.Common
{
    public class ExternalQueryExecuteHandler
    {
        private IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public ExternalQueryExecuteHandler(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public class QueryMouserCartPriceReqVO
        {
            public string PartNumber { get; set; } = "";
            public int Quantity { get; set; }
        }

        public async Task<QueryActionResultRspVO> QueryMouserAction(QueryMouserCartPriceReqVO input)
        {
            var results = new List<QueryActionResultRspVO.QueryResultRspVO>();
            List<QueryActionResultRspVO.DecisionLogVO> decisionLogs = new List<QueryActionResultRspVO.DecisionLogVO>();
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var apiUrl    = _config["ExternalQueryExecuteUrls:Mouser"];
                string apiKey = _config["MouserSearchApiKey"] ?? "";
                string cartKey = _config["MouserCartApiKey"]  ?? "";
                // ── Step 1：搜尋料號，過濾有庫存的所有結果 ──
                var searchReq = new MouserQueryPriceReqVO
                {
                    SearchByPartRequest = new MouserQueryPriceReqVO.SearchByPart
                    {
                        mouserPartNumber = input.PartNumber,
                    }
                };

                var searchResp = await client.PostAsJsonAsync(
                    $"{apiUrl}search/partnumber?apiKey={apiKey}", searchReq);


                if (!searchResp.IsSuccessStatusCode)
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                        Step = BomFileDecisionLogStepEnum.MouserQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation1.GetDescription()} API有誤 \n 回應狀態碼: {(int)searchResp.StatusCode} \n 回應內容: {searchResp.ReasonPhrase}"
                    });
                    return new QueryActionResultRspVO
                    {
                        Result = null,
                        DecisionLogs = decisionLogs
                    };
                }

                var searchResult = await searchResp.Content
                    .ReadFromJsonAsync<MouserPartNumberSearchResultsRspVO>();

                if (searchResult!=null && searchResult.Errors.Any())
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                        Step = BomFileDecisionLogStepEnum.MouserQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation1.GetDescription()} API有誤 \n 回應內容(Errors): {JsonConvert.SerializeObject(searchResult.Errors)}"
                    });
                    return new QueryActionResultRspVO
                    {
                        Result = null,
                        DecisionLogs = decisionLogs
                    };
                }
                    

                // 過濾有庫存的所有結果（與 PHP array_filter 一致）
                if(searchResult!=null && searchResult.SearchResults.Parts.Count == 0)
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                        Step = BomFileDecisionLogStepEnum.MouserQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation1.GetDescription()} API成功\n 無對應的Mouser料號"
                    });
                    return new QueryActionResultRspVO
                    {
                        Result = null,
                        DecisionLogs = decisionLogs
                    };
                }
                var parts = searchResult.SearchResults.Parts
                    .Where(p => p.AvailabilityInStock > 0)
                    .ToList();

                if (!parts.Any())
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                        Step = BomFileDecisionLogStepEnum.MouserQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation1.GetDescription()} API成功\n 無庫存的Mouser料號"
                    });
                    return new QueryActionResultRspVO
                    {
                        Result = null,
                        DecisionLogs = decisionLogs
                    };
                }
                else
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                        Step = BomFileDecisionLogStepEnum.MouserQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation1.GetDescription()} API成功\n 返回結果:{JsonConvert.SerializeObject(parts)}"
                    });
                    List<int> removePartIdxList = new();
                    for(int i = 0; i < parts.Count; i++)
                    {   
                        var p = parts[i];
                        if (p.AvailabilityInStock < input.Quantity)
                        {
                            decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                            {
                                Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                                Step = BomFileDecisionLogStepEnum.MouserQuotation1.ToInt(),
                                Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation1.GetDescription()} API成功\n 但料號 {p.MouserPartNumber} 的庫存量 {p.AvailabilityInStock} 不足以滿足需求數量 {input.Quantity}，將不納入後續加入購物車流程"
                            });
                            removePartIdxList.Add(i);
                        }
                    }
                    removePartIdxList.ForEach(idx => parts.RemoveAt(idx));
                }


                    // ── Step 2：對每一筆 part 各自加入購物車取得實際下單價 ──
                    string sessionCartKey = "";

                foreach (var part in parts)
                {
                    // 從 PriceBreaks 找到 ≤ 目標數量 的最大 break
                    // 若連最小 break 都達不到，則跳過此筆不納入結果
                    var applicableBreak = part.PriceBreaks
                        .Where(b => b.Quantity <= input.Quantity)
                        .LastOrDefault();

                    if (applicableBreak == null) continue;

                    int moq = applicableBreak.Quantity;

                    decimal? actualUnitPrice = null;
                    var cartInsertResp = await client.PostAsJsonAsync(
                        $"{apiUrl}cart/items/insert?apiKey={cartKey}&countryCode=US" +
                        (sessionCartKey != "" ? $"&cartKey={Uri.EscapeDataString(sessionCartKey)}" : ""),
                        new CartItemInsertReqVO
                        {
                            CartItems = new List<CartItemInsertReqVO.CartItem>
                            {
                                new CartItemInsertReqVO.CartItem
                                {
                                    MouserPartNumber   = part.MouserPartNumber,
                                    Quantity           = input.Quantity,
                                    CustomerPartNumber = ""
                                }
                            }
                        });
                    bool carInsertSuccess = false;
                    if (cartInsertResp.IsSuccessStatusCode)
                    {
                        var cartResult = await cartInsertResp.Content
                            .ReadFromJsonAsync<CartItemInsertRspVO>();

                        if (cartResult != null)
                        {
                            if (!cartResult.Errors.Any())
                            {
                                if (!string.IsNullOrEmpty(cartResult.CartKey))
                                    sessionCartKey = cartResult.CartKey;

                                var matched = cartResult.CartItems
                                    ?.FirstOrDefault(i =>
                                        string.Equals(i.MouserPartNumber, part.MouserPartNumber,
                                            StringComparison.OrdinalIgnoreCase)
                                        && !i.Errors.Any());

                                if (matched != null)
                                {
                                    // 差異3：對應 JS fallback 鏈 UnitPrice ?? Price ?? ExtendedPrice
                                    actualUnitPrice = matched.UnitPrice != 0 ? matched.UnitPrice
                                                   : matched.Price != 0 ? matched.Price
                                                   : matched.ExtendedPrice != 0 ? matched.ExtendedPrice
                                                   : (decimal?)null;
                                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                                    {
                                        Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                                        Step = BomFileDecisionLogStepEnum.MouserQuotation2.ToInt(),
                                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation2.GetDescription()} API成功\n 返回結果:{JsonConvert.SerializeObject(cartResult)}"
                                    });
                                    carInsertSuccess = true;
                                }
                                else
                                {
                                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                                    {
                                        Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                                        Step = BomFileDecisionLogStepEnum.MouserQuotation2.ToInt(),
                                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation2.GetDescription()} API成功 \n 無法從回應中找到對應的品項或該品項有錯誤 \n 回應內容:{JsonConvert.SerializeObject(cartResult)}"
                                    });
                                }
                            }
                            else
                            {
                                decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                                {
                                    Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                                    Step = BomFileDecisionLogStepEnum.MouserQuotation2.ToInt(),
                                    Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation2.GetDescription()} API有誤 \n 回應內容(Errors): {JsonConvert.SerializeObject(cartResult)}"
                                });
                            }
                        }
                        
                    }
                    else
                    {
                        decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                        {
                            Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                            Step = BomFileDecisionLogStepEnum.MouserQuotation2.ToInt(),
                            Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation2.GetDescription()} API有誤 \n 回應狀態碼: {(int)cartInsertResp.StatusCode} \n 回應內容: {cartInsertResp.ReasonPhrase}"
                        });
                        continue; // 單筆加入購物車失敗，繼續嘗試下一筆
                    }

                    // ── 清理：移除本次加入的品項 ──
                    if (!string.IsNullOrEmpty(sessionCartKey))
                    {
                        var cartDeleteResp = await client.PostAsJsonAsync(
                                $"{apiUrl}cart/item/remove?apiKey={cartKey}" +
                                $"&cartKey={Uri.EscapeDataString(sessionCartKey)}" +
                                $"&mouserPartNumber={Uri.EscapeDataString(part.MouserPartNumber)}",new { });

                        if (cartDeleteResp.IsSuccessStatusCode)
                        {
                            var cartDeleteResult = await cartDeleteResp.Content
                            .ReadFromJsonAsync<CartItemDeleteRspVO>();
                            if (cartDeleteResult != null && !cartDeleteResult.Errors.Any())
                            {
                                decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                                {
                                    Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                                    Step = BomFileDecisionLogStepEnum.MouserQuotation3.ToInt(),
                                    Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation3.GetDescription()} API成功\n 成功從購物車移除品項 \n 回應內容: {JsonConvert.SerializeObject(cartDeleteResult)}"
                                });
                            }
                            else
                            {
                                decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                                {
                                    Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                                    Step = BomFileDecisionLogStepEnum.MouserQuotation3.ToInt(),
                                    Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation3.GetDescription()} API有誤\n 回應內容(Errors): {JsonConvert.SerializeObject(cartDeleteResult)}"
                                });
                            }
                        }
                        else
                        {
                            decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                            {
                                Stage = BomFileDecisionLogStageEnum.MouserQuotation.ToInt(),
                                Step = BomFileDecisionLogStepEnum.MouserQuotation3.ToInt(),
                                Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.MouserQuotation3.GetDescription()} API有誤 \n 回應狀態碼: {(int)cartDeleteResp.StatusCode} \n 回應內容: {cartDeleteResp.ReasonPhrase}"
                            });
                        }
                    }

                    // ── 組裝單筆結果：優先購物車實際價，fallback 為定價 ──
                    decimal? unitPrice = actualUnitPrice;
                    if (unitPrice == null && applicableBreak != null)
                    {
                        var priceStr = applicableBreak.Price.Replace("$", "").Replace(",", "").Trim();
                        if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                        {
                            unitPrice = parsed;
                        }
                    }
                    if (carInsertSuccess)
                    {
                        results.Add(new QueryActionResultRspVO.QueryResultRspVO
                        {
                            QuotationDate = DateTime.Now,
                            UnitPriceOriginalCurrency = unitPrice,
                            Moq = moq,
                            SupplierName = "MOUSER"
                        });
                    }

                }

                return new QueryActionResultRspVO
                {
                    Result = results.MinBy(p => p.UnitPriceTwd ?? 0),
                    DecisionLogs = decisionLogs
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // ══════════════════════════════════════════════════════
        //  DigiKey 查詢（對應 PHP dk_search 邏輯）
        // ══════════════════════════════════════════════════════
        public async Task<QueryActionResultRspVO> QueryDkAction(QueryMouserCartPriceReqVO input)
        {
            var results    = new List<QueryActionResultRspVO.QueryResultRspVO>();
            var decisionLogs = new List<QueryActionResultRspVO.DecisionLogVO>();
            try
            {
                using var client = _httpClientFactory.CreateClient();
                string clientId     = _config["DkClientId"]     ?? "";
                string clientSecret = _config["DkClientSecret"] ?? "";
                string tokenApiUrl        = _config["ExternalQueryExecuteUrls:DkToken"] ?? "";
                string queryApiUrl        = _config["ExternalQueryExecuteUrls:DkQuery"] ?? "";

                // ── Step 1：取得 OAuth Token ──
                var tokenResp = await client.PostAsync(
                    tokenApiUrl,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"]    = "client_credentials",
                        ["client_id"]     = clientId,
                        ["client_secret"] = clientSecret,
                    }));

                if (!tokenResp.IsSuccessStatusCode)
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage   = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                        Step    = BomFileDecisionLogStepEnum.DkQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.DkQuotation1.GetDescription()} 失敗 \n 狀態碼:{(int)tokenResp.StatusCode}"
                    });
                    return new QueryActionResultRspVO { Result = null, DecisionLogs = decisionLogs };
                }

                var tokenResult = await tokenResp.Content.ReadFromJsonAsync<DkTokenRspVO>();
                string token = tokenResult?.access_token ?? "";
                if (string.IsNullOrEmpty(token))
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage   = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                        Step    = BomFileDecisionLogStepEnum.DkQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.DkQuotation1.GetDescription()} 成功，但 access_token 為空"
                    });
                    return new QueryActionResultRspVO { Result = null, DecisionLogs = decisionLogs };
                }

                decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                {
                    Stage   = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                    Step    = BomFileDecisionLogStepEnum.DkQuotation1.ToInt(),
                    Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.DkQuotation1.GetDescription()} 成功"
                });

                // 建立帶有 DK 標頭的請求函式
                HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content = null)
                {
                    var req = new HttpRequestMessage(method, url) { Content = content };
                    req.Headers.TryAddWithoutValidation("Authorization",          $"Bearer {token}");
                    req.Headers.TryAddWithoutValidation("X-DIGIKEY-Client-Id",    clientId);
                    req.Headers.TryAddWithoutValidation("X-DIGIKEY-Locale-Site",     "US");
                    req.Headers.TryAddWithoutValidation("X-DIGIKEY-Locale-Currency", "USD");
                    req.Headers.TryAddWithoutValidation("X-DIGIKEY-Locale-Language", "en");
                    req.Headers.TryAddWithoutValidation("X-DIGIKEY-Customer-Id",     "");
                    return req;
                }

                // ── Step 2：Keyword 搜尋，過濾有庫存結果 ──
                var searchBody = JsonConvert.SerializeObject(new DkKeywordSearchReqVO
                {
                    Keywords             = input.PartNumber,
                    RecordCount          = 20,
                    RecordStartPosition  = 0,
                    MarketPlaceOptions   = "IncludeMarketPlace"
                });

                var searchResp = await client.SendAsync(
                    BuildRequest(HttpMethod.Post,
                        queryApiUrl+"/search/keyword",
                        new StringContent(searchBody, System.Text.Encoding.UTF8, "application/json")));

                if (!searchResp.IsSuccessStatusCode)
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage   = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                        Step    = BomFileDecisionLogStepEnum.DkQuotation2.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.DkQuotation2.GetDescription()} 失敗 \n 狀態碼:{(int)searchResp.StatusCode} \n {searchResp.ReasonPhrase}"
                    });
                    return new QueryActionResultRspVO { Result = null, DecisionLogs = decisionLogs };
                }

                var searchResult = await searchResp.Content.ReadFromJsonAsync<DkKeywordSearchRspVO>();

                // PHP：優先用 ExactMatches，否則用 Products
                var prods = (searchResult?.ExactMatches?.Count > 0)
                    ? searchResult.ExactMatches
                    : searchResult?.Products ?? new();

                // 過濾有庫存（PHP array_filter），且庫存量必須大於需求量
                prods = prods
                    .Where(p => p.QuantityAvailable >0)
                    .ToList();

                if (!prods.Any())
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage   = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                        Step    = BomFileDecisionLogStepEnum.DkQuotation2.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.DkQuotation2.GetDescription()} 成功，無符合條件或有庫存的料件"
                    });
                    return new QueryActionResultRspVO { Result = null, DecisionLogs = decisionLogs };
                }
                else
                {
                    decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                    {
                        Stage = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                        Step = BomFileDecisionLogStepEnum.DkQuotation1.ToInt(),
                        Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.DkQuotation1.GetDescription()} API成功\n 返回結果:{JsonConvert.SerializeObject(prods)}"
                    });
                    List<int> removePartIdxList = new();
                    for (int i = 0; i < prods.Count; i++)
                    {
                        var p = prods[i];
                        if (p.QuantityAvailable < input.Quantity)
                        {
                            decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                            {
                                Stage = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                                Step = BomFileDecisionLogStepEnum.DkQuotation1.ToInt(),
                                Message = $"輸入參數:{JsonConvert.SerializeObject(input)} \n {BomFileDecisionLogStepEnum.DkQuotation1.GetDescription()} API成功\n 但料號 {p.ProductVariations?.FirstOrDefault()?.DigiKeyProductNumber} 的庫存量 {p.QuantityAvailable} 不足以滿足需求數量 {input.Quantity}，將不納入後續加入購物車流程"
                            });
                            removePartIdxList.Add(i);
                        }
                    }
                    removePartIdxList.ForEach(idx => prods.RemoveAt(idx));
                }
                   

                // ── Step 3：對每筆取 ProductDetail（含 MyPricing 優惠價） ──
                var finalProds = new List<DkProductVO>();
                foreach (var p in prods)
                {
                    // PHP：取 ProductVariations[0].DigiKeyProductNumber 查詳細
                    string? dkn = p.ProductVariations?.FirstOrDefault()?.DigiKeyProductNumber;
                    if (string.IsNullOrEmpty(dkn))
                    {
                        finalProds.Add(p);
                        continue;
                    }

                    var detailResp = await client.SendAsync(
                        BuildRequest(HttpMethod.Get,
                            $"{queryApiUrl}/search/{Uri.EscapeDataString(dkn)}/productdetails"));

                    if (detailResp.IsSuccessStatusCode)
                    {
                        var detail = await detailResp.Content.ReadFromJsonAsync<DkProductDetailRspVO>();
                        finalProds.Add(detail?.Product ?? p);
                        decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                        {
                            Stage   = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                            Step    = BomFileDecisionLogStepEnum.DkQuotation3.ToInt(),
                            Message = $"DK# {dkn} {BomFileDecisionLogStepEnum.DkQuotation3.GetDescription()} 成功"
                        });
                    }
                    else
                    {
                        finalProds.Add(p);
                        decisionLogs.Add(new QueryActionResultRspVO.DecisionLogVO
                        {
                            Stage   = BomFileDecisionLogStageEnum.DkQuotation.ToInt(),
                            Step    = BomFileDecisionLogStepEnum.DkQuotation3.ToInt(),
                            Message = $"DK# {dkn} {BomFileDecisionLogStepEnum.DkQuotation3.GetDescription()} 失敗，使用搜尋結果 fallback。狀態碼:{(int)detailResp.StatusCode}"
                        });
                    }
                }

                // ── 組裝結果：每筆取 ≤ Quantity 的最大 break，優先 MyPricing ──
                foreach (var prod in finalProds)
                {
                    foreach (var variation in prod.ProductVariations ?? new())
                    {
                        if (variation.QuantityAvailableforPackageType < input.Quantity) continue;

                        // MyPricing 優先（合約優惠價），fallback 到 StandardPricing
                        var pricing = (variation.MyPricing?.Count > 0)
                            ? variation.MyPricing
                            : variation.StandardPricing;

                        if (pricing == null || !pricing.Any()) continue;

                        // 找 ≤ 目標數量 的最大 break（與 PHP getApplicablePrice 一致）
                        // 若連最小 break 都達不到，則不納入結果
                        var applicableBreak = pricing
                            .Where(b => b.BreakQuantity <= input.Quantity)
                            .MaxBy(b => b.BreakQuantity);

                        if (applicableBreak == null) continue;

                        results.Add(new QueryActionResultRspVO.QueryResultRspVO
                        {
                            QuotationDate             = DateTime.Now,
                            UnitPriceOriginalCurrency = applicableBreak.UnitPrice,
                            Moq                       = applicableBreak.BreakQuantity,
                            SupplierName              = "DIGIKEY"
                        });
                    }
                }

                return new QueryActionResultRspVO
                {
                    Result       = results.MinBy(p => p.UnitPriceTwd ?? p.UnitPriceOriginalCurrency ?? 0),
                    DecisionLogs = decisionLogs
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
