using Business.DomainModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.AI
{
    /// <summary>
    /// 根據歷史採購紀錄判斷排單價，取得最近一次排單價採購紀錄
    /// </summary>
    public class OrderPriceDecison
    {
        private const string SystemPrompt = @"
你是採購價格分析專家。

輸入為 JSON 陣列，每筆包含：
- unit_price（單價）
- count（出現次數，代表該價格的權重）

請依以下規則分析價格分布：

【分群規則】
1. 將資料依 unit_price 由小到大排序
2. 將 count 視為權重（等同於該價格重複出現 count 次）
3. 根據價格分布進行分群：
   - 若價格明顯存在跳躍（gap），則依 gap 分為低、高群
   - 若無明顯 gap，則以整體分布判斷：
     - 偏低區間為低價群
     - 偏高為高價群

【低價群定義】
- 價格較低且分布集中
- 排除明顯高價（急單 / 異常值）

【高價群定義】
- 價格較高，通常代表急單價或異常高價
- 不在低價群範圍內的所有資料

【例外情況】
若符合以下任一條件，視為無法判定：
- 所有價格非常接近（無法分群）
- 僅有一種價格
- 有效資料（count > 0）不足 2 筆

【輸出規則（嚴格遵守）】
- 只能輸出純 JSON，不得有任何說明文字
- 不得使用 markdown
- 第一個字元必須是 {，最後一個字元必須是 }

判定成功時輸出：
{""Find"":true,""low_min"":低價群最低價數字,""low_max"":低價群最高價數字,""high_min"":高價群最低價數字,""high_max"":高價群最高價數字}

無法判定時輸出：
{""Find"":false,""low_min"":null,""low_max"":null,""high_min"":null,""high_max"":null}
";

        private readonly Kernel _kernel;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="kernel">Semantic Kernel 實例</param>
        public OrderPriceDecison(Kernel kernel)
        {
            _kernel = kernel;
        }

        private class PriceReq
        {
            public decimal unit_price { get; set; }
            public int count { get; set; }
        }

        private class PriceRes
        {
            [JsonPropertyName("Find")]
            public bool Find { get; set; } = false;

            [JsonPropertyName("low_min")]
            public decimal? low_min { get; set; }

            [JsonPropertyName("low_max")]
            public decimal? low_max { get; set; }

            [JsonPropertyName("high_min")]
            public decimal? high_min { get; set; }

            [JsonPropertyName("high_max")]
            public decimal? high_max { get; set; }
        }

        /// <summary>
        /// 以 AI 分析歷史採購價格後回傳最近一次排單價採購紀錄
        /// 呼叫前應已透過 SearchVO（UnitCostLcyGt / Description2In）完成過濾
        /// </summary>
        /// <param name="history">已過濾的歷史採購紀錄清單</param>
        /// <returns>內部採購紀錄 VO，查無資料時回傳空白的 VO</returns>
        public async Task<Models.InternalPurchaseRecordVO> Search(List<SanderModulePurchaseLineDM> history)
        {
            Models.InternalPurchaseRecordVO result = new();

            List<PriceReq> reqs = history
                .GroupBy(x => x.UnitCostLcy)
                .OrderBy(x => x.Key)
                .Select(g => new PriceReq
                {
                    unit_price = g.Key ?? 0,
                    count = g.Count()
                })
                .ToList();

            result.HistoryCount = history.Count;

            if (reqs.Count == 0)
            {
                result.Remark += "查無有效採購價格紀錄\n";
                return result;
            }

            result.HistoryData = Newtonsoft.Json.JsonConvert.SerializeObject(reqs);

            SanderModulePurchaseLineDM? latest;

            if (history.Count == 1)
            {
                result.Remark += "僅有一筆採購紀錄\n";
                latest = history.First();
            }
            else
            {
                ChatHistory chatMessages = new(SystemPrompt);

                GeminiPromptExecutionSettings settings = new()
                {
                    Temperature = 0,
                    TopK = 1,
                    CandidateCount = 1
                };

                IChatCompletionService chatService = _kernel.GetRequiredService<IChatCompletionService>();

                chatMessages.AddUserMessage(JsonSerializer.Serialize(reqs, _jsonOptions));

                ChatMessageContent? response = await chatService.GetChatMessageContentAsync(chatMessages, executionSettings: settings, kernel: _kernel);
                string content = response?.Content ?? string.Empty;

                PriceRes? res = Newtonsoft.Json.JsonConvert.DeserializeObject<PriceRes>(content);

                ArgumentNullException.ThrowIfNull(res, "AI 回傳結果解析失敗");

                if (!res.Find || res.low_min == null || res.low_max == null)
                {
                    result.Remark += "價格皆接近，無法明確分群\n";
                    latest = history
                        .OrderByDescending(x => x.DocumentDate)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();
                }
                else
                {
                    result.Remark += $"價格可明確分為高低價群；低價群範圍：{res.low_min}~{res.low_max}，高價群範圍：{res.high_min}~{res.high_max}\n";
                    result.InternalLowMinPrice = res.low_min;
                    result.InternalLowMaxPrice = res.low_max;
                    result.InternalHighMinPrice = res.high_min;
                    result.InternalHighMaxPrice = res.high_max;
                    latest = history
                        .Where(x => x.UnitCostLcy >= res.low_min && x.UnitCostLcy <= res.low_max)
                        .OrderByDescending(x => x.DocumentDate)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();
                }
            }

            if (latest == null)
                return result;

            result.PurchaseOrderDate = latest.DocumentDate;
            result.UnitPriceOriginalCurrency = latest.UnitCost;
            result.UnitPriceTWD = latest.UnitCostLcy;
            result.Quantity = latest.Quantity;
            result.Currency = latest.CurrencyCode;
            result.SupplierName = latest.BuyFromVendorName;

            return result;
        }
    }
}
