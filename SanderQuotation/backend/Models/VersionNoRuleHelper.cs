using System.Text;
using System.Text.Json;

namespace backend.Models
{
    #region -- 資料結構 --

    /// <summary>
    /// 版號區段設定（RuleSetting JSON 中的單一區段）。
    /// <br/>Type 識別此區段種類，Options 附帶各型別專屬的 key-value 設定。
    /// </summary>
    public class SegmentConfig
    {
        /// <summary>排列順序（由小到大依序產生版號字串）</summary>
        public int Order { get; set; }

        /// <summary>
        /// 區段型別識別字。
        /// 內建型別：MAJOR / MINOR / PATCH / DATE / TIME / FIXED / SEP / RECIPE_NO
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>區段選項，不同 Type 有不同 key-value 組合（詳見各產生器說明）</summary>
        public Dictionary<string, string> Options { get; set; } = new();
    }

    /// <summary>
    /// 版號規則設定物件（序列化後儲存於 TB_RecipeVersionNoRule.RuleSetting）。
    /// </summary>
    public class RuleSettingData
    {
        /// <summary>組成版號的區段清單，依 Order 排序後逐一產生字串並串接</summary>
        public List<SegmentConfig> Segments { get; set; } = new();
    }

    /// <summary>
    /// 版號種子資訊（序列化後儲存於 TB_RecipeVersion.VersionSeed）。
    /// <br/>記錄產生本版號時各計數區段的實際值，以便下一次提交時計算新版號。
    /// </summary>
    public class VersionSeedData
    {
        /// <summary>MAJOR 計數器目前值</summary>
        public int MAJOR { get; set; }

        /// <summary>MINOR 計數器目前值</summary>
        public int MINOR { get; set; }

        /// <summary>PATCH 計數器目前值</summary>
        public int PATCH { get; set; }

        /// <summary>流水號計數器目前值（每次提交均遞增）</summary>
        public int SEQ { get; set; }
    }

    #endregion -- 資料結構 --

    #region -- Strategy Pattern：ISegmentGenerator 介面與具體實作 --

    /// <summary>
    /// 版號區段產生器介面（Strategy）。
    /// <br/>每種區段型別各自實作此介面，新增型別時只需加入新的實作並呼叫
    /// <see cref="SegmentGeneratorRegistry.Register"/>，不影響現有任何區段的邏輯。
    /// </summary>
    public interface ISegmentGenerator
    {
        /// <summary>此產生器負責的型別識別字（不分大小寫）</summary>
        string SegmentType { get; }

        /// <summary>
        /// 依選項與種子資訊，產生此區段的字串片段。
        /// </summary>
        /// <param name="options">來自 <see cref="SegmentConfig.Options"/> 的設定</param>
        /// <param name="seed">目前版本的種子值（含 MAJOR/MINOR/PATCH/SEQ）</param>
        /// <param name="bumpType">本次提交類型（MAJOR / MINOR / PATCH），部分型別不使用此參數</param>
        /// <param name="recipeNo">配方編號（RECIPE_NO 型別使用，其餘可傳 null）</param>
        /// <returns>此區段對應的字串片段</returns>
        string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo);

        /// <summary>回傳此型別的預設選項（用於前端新增區段時的初始值）</summary>
        Dictionary<string, string> GetDefaultOptions();
    }

    #region -- 計數型區段基底（MAJOR / MINOR / PATCH） --

    /// <summary>
    /// MAJOR、MINOR、PATCH 計數型區段的抽象基底。
    /// <br/>共用格式化邏輯（整數 / 字母 / 補位）。
    /// <br/>子類別只需實做 <see cref="SegmentType"/> 和 <see cref="GetCounterValue"/>。
    /// </summary>
    public abstract class VersionCounterSegmentBase : ISegmentGenerator
    {
        /// <inheritdoc/>
        public abstract string SegmentType { get; }

        /// <summary>從種子物件取出對應計數值</summary>
        protected abstract int GetCounterValue(VersionSeedData seed);

        /// <inheritdoc/>
        public string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo)
        {
            int value = GetCounterValue(seed);
            string format = options.GetValueOrDefault("Format", "Integer");
            int digits = int.TryParse(options.GetValueOrDefault("Digits", "1"), out int d) ? d : 1;
            char padChar = options.GetValueOrDefault("PadChar", "0").FirstOrDefault('0');

            string raw = format.Equals("Letter", StringComparison.OrdinalIgnoreCase)
                ? ToLetterFormat(value)
                : value.ToString();

            return raw.PadLeft(digits, padChar);
        }

        /// <inheritdoc/>
        public Dictionary<string, string> GetDefaultOptions() => new()
        {
            { "StartValue", "0" },
            { "Format",     "Integer" },
            { "MaxValue",   "99" },
            { "Digits",     "1" },
            { "PadChar",    "0" },
        };

        /// <summary>
        /// 將整數轉為 Excel 欄位命名風格的字母序列。
        /// <br/>1 → A, 2 → B, …, 26 → Z, 27 → AA, 28 → AB, …
        /// </summary>
        private static string ToLetterFormat(int value)
        {
            if (value <= 0) return "A";
            StringBuilder sb = new();
            while (value > 0)
            {
                value--;                          // 轉為 0-based
                sb.Insert(0, (char)('A' + value % 26));
                value /= 26;
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// MAJOR（主版號）區段產生器。
    /// <br/>VersionBumpType = MAJOR 時此計數器遞增，MINOR 與 PATCH 同時重置至其起始值。
    /// </summary>
    public class MajorSegmentGenerator : VersionCounterSegmentBase
    {
        /// <inheritdoc/>
        public override string SegmentType => "MAJOR";

        /// <inheritdoc/>
        protected override int GetCounterValue(VersionSeedData seed) => seed.MAJOR;
    }

    /// <summary>
    /// MINOR（次版號）區段產生器。
    /// <br/>VersionBumpType = MINOR 時此計數器遞增，PATCH 同時重置至其起始值。
    /// </summary>
    public class MinorSegmentGenerator : VersionCounterSegmentBase
    {
        /// <inheritdoc/>
        public override string SegmentType => "MINOR";

        /// <inheritdoc/>
        protected override int GetCounterValue(VersionSeedData seed) => seed.MINOR;
    }

    /// <summary>
    /// PATCH（修訂號）區段產生器。
    /// <br/>VersionBumpType = PATCH 時此計數器遞增。
    /// </summary>
    public class PatchSegmentGenerator : VersionCounterSegmentBase
    {
        /// <inheritdoc/>
        public override string SegmentType => "PATCH";

        /// <inheritdoc/>
        protected override int GetCounterValue(VersionSeedData seed) => seed.PATCH;
    }

    #endregion -- 計數型區段基底（MAJOR / MINOR / PATCH） --

    #region -- 日期 / 時間 區段 --

    /// <summary>
    /// 日期區段產生器。
    /// <br/>Options["DateFormat"]：日期格式樣板。
    /// <list type="bullet">
    ///   <item>YYYY → 西元年（例：2026）</item>
    ///   <item>YYY  → 民國年（例：115）</item>
    ///   <item>MM   → 月份（01-12）</item>
    ///   <item>DD   → 日期（01-31）</item>
    /// </list>
    /// 範例：DateFormat = "YYYY" → "2026"
    /// </summary>
    public class DateSegmentGenerator : ISegmentGenerator
    {
        /// <inheritdoc/>
        public string SegmentType => "DATE";

        /// <inheritdoc/>
        public string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo)
        {
            string fmt = options.GetValueOrDefault("DateFormat", "YYYY");
            DateTime now = DateTime.Now;
            string rocYear = (now.Year - 1911).ToString("D3");

            return fmt
                .Replace("YYYY", now.Year.ToString("D4"))
                .Replace("YYY", rocYear)
                .Replace("MM", now.Month.ToString("D2"))
                .Replace("DD", now.Day.ToString("D2"));
        }

        /// <inheritdoc/>
        public Dictionary<string, string> GetDefaultOptions() => new()
        {
            { "DateFormat", "YYYY" },
        };
    }

    /// <summary>
    /// 時間區段產生器。
    /// <br/>Options["TimeFormat"]：時間格式樣板。
    /// <list type="bullet">
    ///   <item>HH → 24 小時制小時（00-23）</item>
    ///   <item>MM → 分鐘（00-59）</item>
    /// </list>
    /// 範例：TimeFormat = "HHMM" → "1432"
    /// </summary>
    public class TimeSegmentGenerator : ISegmentGenerator
    {
        /// <inheritdoc/>
        public string SegmentType => "TIME";

        /// <inheritdoc/>
        public string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo)
        {
            string fmt = options.GetValueOrDefault("TimeFormat", "HH");
            DateTime now = DateTime.Now;
            return fmt
                .Replace("HH", now.Hour.ToString("D2"))
                .Replace("MM", now.Minute.ToString("D2"))
                .Replace("SS", now.Second.ToString("D2"));
        }

        /// <inheritdoc/>
        public Dictionary<string, string> GetDefaultOptions() => new()
        {
            { "TimeFormat", "HH" },
        };
    }

    #endregion -- 日期 / 時間 區段 --

    #region -- 其他區段 --

    /// <summary>
    /// 固定文字區段產生器。直接輸出 Options["Text"] 的字串值。
    /// </summary>
    public class FixedSegmentGenerator : ISegmentGenerator
    {
        /// <inheritdoc/>
        public string SegmentType => "FIXED";

        /// <inheritdoc/>
        public string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo)
            => options.GetValueOrDefault("Text", "");

        /// <inheritdoc/>
        public Dictionary<string, string> GetDefaultOptions() => new()
        {
            { "Text", "V" },
        };
    }

    /// <summary>
    /// 分隔符號區段產生器。輸出 Options["Symbol"] 指定的符號（"-" / "." / "_" / "/"）。
    /// </summary>
    public class SeparatorSegmentGenerator : ISegmentGenerator
    {
        /// <inheritdoc/>
        public string SegmentType => "SEP";

        /// <inheritdoc/>
        public string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo)
            => options.GetValueOrDefault("Symbol", ".");

        /// <inheritdoc/>
        public Dictionary<string, string> GetDefaultOptions() => new()
        {
            { "Symbol", "." },
        };
    }

    /// <summary>
    /// 配方編號區段產生器。直接輸出傳入的配方編號，無需任何 Options 設定。
    /// </summary>
    public class RecipeNoSegmentGenerator : ISegmentGenerator
    {
        /// <inheritdoc/>
        public string SegmentType => "RECIPE_NO";

        /// <inheritdoc/>
        public string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo)
            => recipeNo ?? "";

        /// <inheritdoc/>
        public Dictionary<string, string> GetDefaultOptions() => new();
    }

    #endregion -- 其他區段 --
    #endregion -- Strategy Pattern：ISegmentGenerator 介面與具體實作 --

    #region -- SeqSegmentGenerator（流水號） --

    /// <summary>
    /// 流水號區段產生器。
    /// <br/>每次提交版本時此計數器固定遞增，與 <c>VersionBumpType</c> 無關。
    /// <br/>Options：
    /// <list type="bullet">
    ///   <item><description><c>StartValue</c> — 起始值（預設 1；種子初始化時使用）</description></item>
    ///   <item><description><c>Digits</c>     — 補位位數（預設 3）</description></item>
    ///   <item><description><c>PadChar</c>    — 補位字元（預設 '0'）</description></item>
    /// </list>
    /// </summary>
    public class SeqSegmentGenerator : ISegmentGenerator
    {
        /// <inheritdoc/>
        public string SegmentType => "SEQ";

        /// <inheritdoc/>
        public string GenerateSegment(
            Dictionary<string, string> options,
            VersionSeedData seed,
            string? bumpType,
            string? recipeNo)
        {
            int digits = int.TryParse(options.GetValueOrDefault("Digits", "3"), out int d) ? d : 3;
            char padChar = options.GetValueOrDefault("PadChar", "0").FirstOrDefault('0');
            return seed.SEQ.ToString().PadLeft(digits, padChar);
        }

        /// <inheritdoc/>
        public Dictionary<string, string> GetDefaultOptions() => new()
        {
            { "StartValue", "1"  },
            { "Digits",     "3"  },
            { "PadChar",    "0"  },
        };
    }

    #endregion -- SeqSegmentGenerator（流水號） --

    #region -- Registry（登錄表） --

    /// <summary>
    /// 區段產生器登錄表（Registry）。
    /// <br/>以 <see cref="ISegmentGenerator.SegmentType"/> 為 key 管理所有已知產生器。
    /// <br/>新增區段型別只需建立新的 <see cref="ISegmentGenerator"/> 實作並呼叫
    /// <see cref="Register"/>，不影響任何既有區段。
    /// </summary>
    public class SegmentGeneratorRegistry
    {
        private readonly Dictionary<string, ISegmentGenerator> _map
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>初始化並自動登錄所有內建產生器</summary>
        public SegmentGeneratorRegistry()
        {
            Register(new MajorSegmentGenerator());
            Register(new MinorSegmentGenerator());
            Register(new PatchSegmentGenerator());
            Register(new DateSegmentGenerator());
            Register(new TimeSegmentGenerator());
            Register(new FixedSegmentGenerator());
            Register(new SeparatorSegmentGenerator());
            Register(new RecipeNoSegmentGenerator());
            Register(new SeqSegmentGenerator());
        }

        /// <summary>登錄（或覆蓋）一個區段產生器</summary>
        public void Register(ISegmentGenerator generator)
            => _map[generator.SegmentType] = generator;

        /// <summary>取得指定型別的產生器；找不到時回傳 null</summary>
        public ISegmentGenerator? Get(string type)
            => _map.TryGetValue(type, out ISegmentGenerator? g) ? g : null;

        /// <summary>取得指定型別的預設選項；找不到時回傳空字典</summary>
        public Dictionary<string, string> GetDefaultOptions(string type)
            => Get(type)?.GetDefaultOptions() ?? new Dictionary<string, string>();

        /// <summary>取得所有已登錄的區段型別識別字</summary>
        public IEnumerable<string> RegisteredTypes => _map.Keys;
    }

    #endregion -- Registry（登錄表） --

    #region -- VersionNoRuleHelper（主 Helper） --

    /// <summary>
    /// 版號規則業務邏輯 Helper。
    /// <br/>提供版號產生、種子計算等功能，供 Controller 呼叫。
    /// <br/>所有方法皆為 static，內部共用 <see cref="Registry"/>。
    /// </summary>
    public static class VersionNoRuleHelper
    {
        /// <summary>
        /// 共用區段產生器登錄表（應用程式生命週期內保持單一實例）。
        /// <br/>可呼叫 <see cref="SegmentGeneratorRegistry.Register"/> 擴充自訂區段型別。
        /// </summary>
        public static readonly SegmentGeneratorRegistry Registry = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        #region -- 版號產生 --

        /// <summary>
        /// 依區段規則與種子資訊，產生版號字串。
        /// <br/>各區段依 <see cref="SegmentConfig.Order"/> 由小至大排序後串接。
        /// 未知型別的區段會被略過，不影響其他區段輸出。
        /// </summary>
        /// <param name="ruleSettingJson">TB_RecipeVersionNoRule.RuleSetting 的 JSON 字串</param>
        /// <param name="seed">本版本的種子值（含 MAJOR/MINOR/PATCH/SEQ）</param>
        /// <param name="bumpType">本次提交類型（MAJOR / MINOR / PATCH），部分區段會使用此值</param>
        /// <param name="recipeNo">配方編號（RECIPE_NO 型別使用，可傳 null）</param>
        /// <returns>完整的版號字串，例如 "V1.2.3"</returns>
        public static string GenerateVersionNo(
            string ruleSettingJson,
            VersionSeedData seed,
            string bumpType,
            string? recipeNo = null)
        {
            RuleSettingData setting = DeserializeRuleSetting(ruleSettingJson);
            StringBuilder sb = new();

            foreach (SegmentConfig seg in setting.Segments.OrderBy(s => s.Order))
            {
                ISegmentGenerator? gen = Registry.Get(seg.Type);
                if (gen == null) continue;   // 未知型別略過，確保可擴充性

                sb.Append(gen.GenerateSegment(seg.Options, seed, bumpType, recipeNo));
            }

            return sb.ToString();
        }

        #endregion -- 版號產生 --

        #region -- 種子計算 --

        /// <summary>
        /// 依提交類型與目前種子，計算下一個版本的種子值。
        /// <br/><b>升版規則（依 VersionBumpType）：</b>
        /// <list type="bullet">
        ///   <item>MAJOR：MAJOR++，MINOR 重置至 StartValue，PATCH 重置至 StartValue</item>
        ///   <item>MINOR：MINOR++，PATCH 重置至 StartValue</item>
        ///   <item>PATCH：PATCH++</item>
        ///   <item>SEQ（流水號）：每次提交均遞增，與 bumpType 無關</item>
        /// </list>
        /// </summary>
        /// <param name="ruleSettingJson">RuleSetting JSON（用以取得各段 StartValue 及是否存在）</param>
        /// <param name="currentSeed">本次提交前（目前最新版本）的種子值</param>
        /// <param name="bumpType">提交類型</param>
        /// <returns>供下一個版本使用的新種子值</returns>
        public static VersionSeedData CalculateNewSeed(
            string ruleSettingJson,
            VersionSeedData currentSeed,
            string bumpType)
        {
            RuleSettingData setting = DeserializeRuleSetting(ruleSettingJson);

            bool hasMajor = HasSegment(setting, "MAJOR");
            bool hasMinor = HasSegment(setting, "MINOR");
            bool hasPatch = HasSegment(setting, "PATCH");
            bool hasSeq = HasSegment(setting, "SEQ");

            // 深拷貝避免修改原物件
            VersionSeedData newSeed = new();
            newSeed.MAJOR = currentSeed.MAJOR;
            newSeed.MINOR = currentSeed.MINOR;
            newSeed.PATCH = currentSeed.PATCH;
            newSeed.SEQ = currentSeed.SEQ;

            switch (bumpType?.ToUpperInvariant())
            {
                case "MAJOR":
                    if (hasMajor) newSeed.MAJOR++;
                    if (hasMinor) newSeed.MINOR = GetSegmentStartValue(setting, "MINOR");
                    if (hasPatch) newSeed.PATCH = GetSegmentStartValue(setting, "PATCH");
                    break;

                case "MINOR":
                    if (hasMinor) newSeed.MINOR++;
                    if (hasPatch) newSeed.PATCH = GetSegmentStartValue(setting, "PATCH");
                    break;

                case "PATCH":
                default:
                    if (hasPatch) newSeed.PATCH++;
                    break;
            }

            if (hasSeq) newSeed.SEQ++;

            return newSeed;
        }

        /// <summary>
        /// 取得規則的初始種子，對應第一個版本所使用的種子值。
        /// <br/>MAJOR/MINOR/PATCH 各取其 Options["StartValue"]，SEQ 從 0 開始。
        /// <br/>當配方尚無任何版本時，以此作為起始種子呼叫 <see cref="GenerateVersionNo"/>。
        /// </summary>
        public static VersionSeedData GetInitialSeed(string ruleSettingJson)
        {
            RuleSettingData setting = DeserializeRuleSetting(ruleSettingJson);
            VersionSeedData seed = new();
            seed.MAJOR = GetSegmentStartValue(setting, "MAJOR");
            seed.MINOR = GetSegmentStartValue(setting, "MINOR");
            seed.PATCH = GetSegmentStartValue(setting, "PATCH");
            seed.SEQ = GetSegmentStartValue(setting, "SEQ");
            return seed;
        }

        /// <summary>
        /// 產生規則預覽版號。
        /// <br/>以初始種子模擬第一個版本的版號，適合在規則設定畫面顯示範例。
        /// </summary>
        /// <param name="ruleSettingJson">RuleSetting JSON</param>
        /// <param name="recipeNo">示範用配方編號，可傳 null（RECIPE_NO 區段將顯示為空）</param>
        /// <returns>模擬的第一個版本版號，例如 "V1.0.0"</returns>
        public static string GeneratePreview(string ruleSettingJson, string? recipeNo = null)
        {
            if (string.IsNullOrWhiteSpace(ruleSettingJson)) return "";
            VersionSeedData seed = GetInitialSeed(ruleSettingJson);
            return GenerateVersionNo(ruleSettingJson, seed, "PATCH", recipeNo ?? "REC-001");
        }

        /// <summary>
        /// 將 VersionSeedData 序列化為 JSON 字串，以便儲存至 TB_RecipeVersion.VersionSeed。
        /// </summary>
        public static string SerializeSeed(VersionSeedData seed)
            => JsonSerializer.Serialize(seed, _jsonOpts);

        /// <summary>
        /// 將 TB_RecipeVersion.VersionSeed 中的 JSON 字串還原為 <see cref="VersionSeedData"/>。
        /// 若字串為空或解析失敗，回傳空種子（各計數器皆為 0）。
        /// </summary>
        public static VersionSeedData DeserializeSeed(string? seedJson)
        {
            if (string.IsNullOrWhiteSpace(seedJson)) return new VersionSeedData();
            try
            {
                return JsonSerializer.Deserialize<VersionSeedData>(seedJson, _jsonOpts)
                    ?? new VersionSeedData();
            }
            catch
            {
                return new VersionSeedData();
            }
        }

        #endregion -- 種子計算 --

        #region -- 私有輔助方法 --

        /// <summary>將 RuleSetting JSON 字串反序列化為 <see cref="RuleSettingData"/>。解析失敗時回傳空物件。</summary>
        private static RuleSettingData DeserializeRuleSetting(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new RuleSettingData();
            try
            {
                return JsonSerializer.Deserialize<RuleSettingData>(json, _jsonOpts)
                    ?? new RuleSettingData();
            }
            catch
            {
                return new RuleSettingData();
            }
        }

        /// <summary>判斷規則中是否存在指定型別的區段。</summary>
        private static bool HasSegment(RuleSettingData setting, string type)
            => setting.Segments.Any(s => s.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        /// <summary>取得指定型別區段的 StartValue；找不到時回傳 0。</summary>
        private static int GetSegmentStartValue(RuleSettingData setting, string type)
        {
            SegmentConfig? seg = setting.Segments
                .FirstOrDefault(s => s.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            if (seg == null) return 0;
            return seg.Options.TryGetValue("StartValue", out string? v)
                   && int.TryParse(v, out int i) ? i : 0;
        }

        #endregion -- 私有輔助方法 --
    }

    #endregion -- VersionNoRuleHelper（主 Helper） --
}
