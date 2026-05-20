using Const.ApiModels.QueryPrice;
using Core.Utility.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Newtonsoft.Json;
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
                }


                    // ── Step 2：對每一筆 part 各自加入購物車取得實際下單價 ──
                    string sessionCartKey = "";

                foreach (var part in parts)
                {
                    // 從 PriceBreaks 找到 ≤ 目標數量 的最大 break
                    var applicableBreak = part.PriceBreaks
                        .Where(b => b.Quantity <= input.Quantity)
                        .LastOrDefault()
                        ?? part.PriceBreaks.FirstOrDefault(); // 低於最小 break 時 fallback 取最小

                    int moq = applicableBreak?.Quantity ?? 1;

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
    }
}
