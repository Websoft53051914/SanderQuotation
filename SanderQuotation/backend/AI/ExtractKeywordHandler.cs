using Business.DomainModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace backend.AI
{
    /// <summary>
    /// 內部料品表 AI 關鍵字抽取的處理器
    /// 負責從 SanderModuleItemDM 資料中提取關鍵字並轉換為 TBSanderModuleItemKeywordDM 結構，並提供比對與標準化功能。
    /// </summary>
    public class ExtractKeywordHandler
    {
        private readonly Kernel _kernel;
        /**
         * 提取關鍵字提示語
         */
        private const string systemPrompt = @"
你是一個電子零件資料解析專家，負責從資料中提取關鍵字並轉換為可用於搜尋與比對的結構。
====================
【輸入資料】
====================
以下為 JSON 格式的 List<SanderModuleItemDM>，每筆包含：
- No：內部料號
- Description：料品規格主欄位
- Description2：料品規格次欄位
- LongDesc：LongDesc
- LongDesc2：LongDesc2
====================
【任務說明】
====================
請針對每一筆資料，依不同欄位進行關鍵字提取，並輸出 JSON 結構。
欄位分為兩類處理：
--------------------
一、LongDesc / LongDesc2
--------------------
目標：提取「廠商料號（MPN）」、「品牌（Manufacturer）」、「供應商型號」
規則：
1. 抓取可能的廠商料號（MPN）
   - 通常為英數符號混合字串（長度 >= 6）
   - 例如：UPM1E102MHD6、EKY-250ELL102MK20S
2. 抓取品牌名稱（若可辨識）
   - 例如：Nichicon、Rubycon、Panasonic
3. 忽略：
   - 中文描述（如：短、腳位外八）
   - 尺寸（如：12.5x20mm）
--------------------
二、Description / Description2
--------------------
目標：提取「料品類型 + 規格參數」，並轉為適合語意搜尋的結構化字串。
【步驟 1】料品類型判斷
可能類型：
- RESISTOR（電阻）
- CAPACITOR（電容）
- INDUCTOR（電感）
- DIODE（二極體）
- IC
- CONNECTOR
- OTHER

【步驟 2】規格抽取
依類型抽取：
- 電阻：阻值、封裝、誤差、功率
- 電容：容量、電壓、溫度、類型（ELECTROLYTIC / CERAMIC）
- 通用：封裝（0603 / 0805 / DIP / SMD）

【步驟 3】單位正規化（強制規則，與查詢端完全一致）

【電容（Capacitance）】
- 允許單位：PF / NF / UF
- 必須自動選擇最適單位，使數值落在 1 ~ 1000 之間
換算規則：
- 1UF = 1000NF
- 1NF = 1000PF
範例：
- 0.1UF → 100NF
- 1000NF → 1UF
- 220PF → 220PF
- 0.00022UF → 220PF

【電阻（Resistance）】
- 統一單位：OHM（完整展開，不使用 K / M 縮寫）
換算規則：
- 1K = 1000OHM
- 1M = 1000000OHM
範例：
- 10K → 10000OHM
- 4.7K → 4700OHM
- 1M → 1000000OHM

【電壓（Voltage）】
- 統一單位：V
換算規則：
- 1KV = 1000V
範例：
- 25V → 25V
- 0.5KV → 500V

【電流（Current）】
- 統一單位：MA / A（自動選擇使數值落在 1~1000）
換算規則：
- 1A = 1000MA
範例：
- 0.5A → 500MA
- 2000MA → 2A

【功率（Power）】
- 統一單位：W
換算規則：
- 1/4W → 0.25W

【溫度（Temperature）】
- 統一單位：C（去除°符號）
範例：
- 105°C → 105C

【封裝（Package）】
- 保持標準格式（0603 / 0805 / DIP / SMD）

【步驟 4】輸出格式
- 將所有資訊合併為單一標準化關鍵字字串
- 使用空白分隔
- 全部大寫
- 數值 + 單位不可有空白（例如：100NF、10000OHM）
- 不可同時存在不同單位（例如：UF + NF）
- 小數最多保留 6 位有效數字，去除不必要尾數（0.100 → 0.1）
- 若欄位中未明確出現某項規格（如溫度、誤差），不得推測或補充
- 僅能輸出輸入文字中明確存在的資訊
範例：
  RESISTOR 10000OHM 0603 1%
  CAPACITOR 100NF 25V 105C ELECTROLYTIC
  CAPACITOR 1000UF 25V ELECTROLYTIC DIP
--------------------
三、輸出規則
--------------------
- No：對應原始資料 No
- ColumnName：來源欄位名稱（Description / Description2 / LongDesc / LongDesc2）
- Keyword：提取出的關鍵字
--------------------
四、重要限制
--------------------
1. 不可輸出重複 Keyword
2. 不可輸出空值
3. 不可產生原始資料不存在的內容（不可幻想）
4. 若欄位無有效資訊，則跳過該欄位
5. LongDesc / LongDesc2 可輸出多筆
6. Description / Description2 僅可各輸出一筆
7. 一律大寫
====================
【輸出限制（最高優先級）】
====================
- 僅允許輸出合法 JSON 陣列
- 輸出必須為純文字（plain text）
- 嚴禁包含任何 Markdown 語法（包含 ```、json）
- 嚴禁添加說明、註解、換行外的額外符號
- 若違反上述任一規則，視為錯誤輸出，請重新產生
JSON 格式如下：
[
  {
    ""No"": """",
    ""ColumnName"": """",
    ""Keyword"": """"
  }
]
";
        /**
         * 關鍵字比對提示語
         */
        private const string systemPrompt2 = @"
你是一個電子零件比對專家，負責判斷「搜尋字串」與「候選關鍵字」在語意上是否完全相同。

====================
【輸入資料】
====================

搜尋字串（SearchKeyword）：
{searchKeyword}

候選資料（前 5 筆 similarity 查詢結果）：
{candidateJson}

資料格式如下：
[
  {{
    """"No"""": """""""",
    """"ColumnName"""": """""""",
    """"Keyword"""": """""""",
    """"SimilarityScore"""": 0.0
  }}
]

====================
【任務】
====================

請針對每一筆候選資料，判斷：

👉 SearchKeyword 與 Keyword 是否「語意完全相同」

如果是：
→ 將 SimilarityScore 設為 1

如果不是：
→ 維持原本 SimilarityScore

====================
【輸出規則（嚴格遵守）】
====================

- 僅輸出 JSON 陣列
- 不得輸出任何說明文字
- 不得使用 markdown
- 不得使用 ``` 或 ```json

- 每筆資料都必須回傳
- 僅允許修改 SimilarityScore，其餘欄位不得變更

JSON 格式如下：

[
  {{
    """"No"""": """""""",
    """"ColumnName"""": """""""",
    """"Keyword"""": """""""",
    """"SimilarityScore"""": 0.0
  }}
]
";
        /**
         * 搜尋字串標準化提示語
         */
        private const string systemPrompt3 = @"
你是一個電子零件資料標準化專家，負責將輸入內容轉換為「可用於搜尋與比對的標準化關鍵字」。
====================
【輸入資料】
====================
""{searchText}""
====================
【任務說明】
====================
請將輸入內容轉換為「標準化關鍵字字串」。
--------------------
一、料品類型判斷
--------------------
可能類型：
- RESISTOR
- CAPACITOR
- INDUCTOR
- DIODE
- IC
- CONNECTOR
- OTHER
--------------------
二、規格抽取
--------------------
依內容抽取：
- 阻值
- 容量
- 電壓
- 封裝（0603 / 0805 / DIP / SMD）
- 誤差
- 溫度
- 電容類型（ELECTROLYTIC / CERAMIC，若明確出現）
--------------------
三、單位正規化（強制規則）
--------------------
所有數值必須轉為「唯一且一致的標準表示法」。

【電容（Capacitance）】
- 允許單位：PF / NF / UF
- 必須自動選擇最適單位，使數值落在 1 ~ 1000 之間
換算規則：
- 1UF = 1000NF
- 1NF = 1000PF
範例：
- 0.1UF → 100NF
- 1000NF → 1UF
- 220PF → 220PF
- 0.00022UF → 220PF

【電阻（Resistance）】
- 統一單位：OHM（完整展開，不使用 K / M 縮寫）
換算規則：
- 1K = 1000OHM
- 1M = 1000000OHM
範例：
- 10K → 10000OHM
- 4.7K → 4700OHM
- 1M → 1000000OHM

【電壓（Voltage）】
- 統一單位：V
換算規則：
- 1KV = 1000V
範例：
- 25V → 25V
- 0.5KV → 500V
- 1KV → 1000V

【電流（Current）】
- 統一單位：MA / A（自動選擇使數值落在 1~1000）
換算規則：
- 1A = 1000MA
範例：
- 0.5A → 500MA
- 2000MA → 2A

【功率（Power）】
- 統一單位：W
換算規則：
- 1/4W → 0.25W

【溫度（Temperature）】
- 統一單位：C（去除°符號）
範例：
- 105°C → 105C

【封裝（Package）】
- 保持標準格式（0603 / 0805 / DIP / SMD）
--------------------
四、輸出格式（重要）
--------------------
- 所有內容轉為單一字串
- 使用空白分隔
- 全部大寫
範例：
RESISTOR 10000OHM 0603 1%
CAPACITOR 100NF 25V 105C
CAPACITOR 220PF 50V
--------------------
五、嚴格限制
--------------------
1. 數值 + 單位不可有空白（例如：100NF）
2. 不可同時存在不同單位（例如：UF + NF）
3. 必須完成所有單位換算
4. 小數最多保留 6 位有效數字
5. 去除不必要尾數（0.100 → 0.1）
6. 不可產生不存在的數值
7. 僅輸出一行結果
8. 若輸入中未明確出現某項規格（例如溫度、誤差、電壓等），則不得推測或補充該規格
9. 嚴禁依常見規格（例如 105C、1%、10% 等）自行推論補齊
10. 僅能輸出輸入文字中「明確存在」的資訊
====================
【回覆規則（嚴格）】
====================
- 只輸出最終關鍵字字串
- 不得包含任何說明文字
- 不得使用 JSON
- 不得使用 markdown
";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="kernel"></param>
        public ExtractKeywordHandler(Kernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// 輸入內部料號，輸出最近一次的排單價採購紀錄
        /// </summary>
        /// <param name="itemList">內部料號資料列表</param>
        /// <returns>提取的關鍵字結果</returns>
        public async Task<List<TBSanderModuleItemKeywordDM>> ExtractKeyword(List<SanderModuleItemDM> itemList)
        {
            try
            {
                string inputJson = JsonSerializer.Serialize(itemList, _jsonOptions);

                ChatHistory chatMessages = new ChatHistory(systemPrompt);
                var settings = new GeminiPromptExecutionSettings
                {
                    Temperature = 0,
                    TopK = 1,
                    CandidateCount = 1,
                };

                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                chatMessages.AddUserMessage(inputJson);

                ChatMessageContent? response = await chatService.GetChatMessageContentAsync(chatMessages, executionSettings: settings, kernel: _kernel);
                string content = response?.Content ?? string.Empty;

                List<TBSanderModuleItemKeywordDM> result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TBSanderModuleItemKeywordDM>>(content) ?? [];

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExtractKeyword: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 判斷「搜尋字串」與「候選關鍵字」在語意上是否完全相同
        /// </summary>
        /// <param name="searchKeyword"></param>
        /// <param name="candidateList"></param>
        /// <returns></returns>
        public async Task<List<TBSanderModuleItemKeywordDM>> MatchKeyword(string searchKeyword, List<TBSanderModuleItemKeywordDM> candidateList)
        {
            try
            {
                string candidateJson = JsonSerializer.Serialize(candidateList, _jsonOptions);
                ChatHistory chatMessages = new ChatHistory(systemPrompt2);
                var settings = new GeminiPromptExecutionSettings
                {
                    Temperature = 0,
                    TopK = 1,
                    CandidateCount = 1,
                };
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                chatMessages.AddUserMessage($"{{searchKeyword}}={searchKeyword}；{{candidateJson}}={candidateJson}");
                ChatMessageContent? response = await chatService.GetChatMessageContentAsync(chatMessages, executionSettings: settings, kernel: _kernel);
                string content = response?.Content ?? string.Empty;
                List<TBSanderModuleItemKeywordDM> result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TBSanderModuleItemKeywordDM>>(content) ?? [];
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MatchKeyword: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 標準化使用者輸入的搜尋字串，轉換為適合語意搜尋的結構化字串
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public async Task<string> StandardizeSearchKeyword(string searchText)
        {
            try
            {
                ChatHistory chatMessages = new ChatHistory(systemPrompt3);
                var settings = new GeminiPromptExecutionSettings
                {
                    Temperature = 0,
                    TopK = 1,
                    CandidateCount = 1,
                };
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                chatMessages.AddUserMessage($"{{searchText}}={searchText}");
                ChatMessageContent? response = await chatService.GetChatMessageContentAsync(chatMessages, executionSettings: settings, kernel: _kernel);
                string content = response?.Content ?? string.Empty;
                return content.Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in StandardizeSearchKeyword: {ex.Message}");
                throw;
            }
        }
    }
}
