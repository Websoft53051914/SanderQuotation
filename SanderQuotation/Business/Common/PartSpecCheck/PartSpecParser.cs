using System.Globalization;
using System.Text.RegularExpressions;

namespace Business.Common.PartSpecCheck
{
    /// <summary>
    /// 自標準化 Keyword 字串解析類型與規格數值（程式解析，不再問 AI）
    /// </summary>
    public static partial class PartSpecParser
    {
        private static readonly string[] KnownCategories =
        [
            "RESISTOR", "CAPACITORS", "CAPACITOR", "INDUCTOR", "OSCILLATOR", "FUSE",
            "IC", "LED", "CONNECTOR", "DIODE", "MODULE", "WIRE", "OTHER"
        ];

        private static readonly HashSet<string> PackageTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "DIP", "SMD", "SOP", "SOIC", "QFN", "BGA", "SOD", "SOD123", "SOD323",
            "SOT23", "SOT223", "SOT89", "TO92", "TO220", "DO35", "DO41", "AXIAL", "RADIAL"
        };

        public static ParsedPartSpec Parse(string? keyword)
        {
            ParsedPartSpec spec = new()
            {
                RawKeyword = keyword?.Trim() ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(keyword))
                return spec;

            string text = keyword.Trim().ToUpperInvariant();
            text = text.Replace('Ω', ' ').Replace("OHMS", "OHM");

            spec.CategoryCode = DetectCategory(text);
            spec.ResistanceOhm = TryParseResistanceOhm(text);
            spec.CapacitancePf = TryParseCapacitancePf(text);
            spec.InductanceNh = TryParseInductanceNh(text);
            spec.FrequencyHz = TryParseFrequencyHz(text);
            spec.CurrentA = TryParseCurrentA(text);
            spec.VoltageV = TryParseVoltageV(text);
            spec.Package = TryParsePackage(text);

            return spec;
        }

        /// <summary>
        /// 以主檔 ItemCategoryCode 覆寫／補齊類型（候選端優先）
        /// </summary>
        public static void ApplyItemCategoryCode(ParsedPartSpec spec, string? itemCategoryCode)
        {
            if (string.IsNullOrWhiteSpace(itemCategoryCode))
                return;

            spec.CategoryCode = itemCategoryCode.Trim().ToUpperInvariant();
        }

        private static string? DetectCategory(string text)
        {
            string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return null;

            string first = tokens[0];
            foreach (string cat in KnownCategories)
            {
                if (string.Equals(first, cat, StringComparison.OrdinalIgnoreCase))
                    return cat.ToUpperInvariant();
            }

            // 字串中出現已知類型 token（非字首時）
            foreach (string cat in KnownCategories)
            {
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(cat)}\b", RegexOptions.IgnoreCase))
                    return cat.ToUpperInvariant();
            }

            return null;
        }

        private static double? TryParseResistanceOhm(string text)
        {
            Match m = ResistanceOhmRegex().Match(text);
            if (m.Success && TryParseDouble(m.Groups[1].Value, out double ohm))
                return ohm;

            // 1R3 / 13R 記法（標準化未完成時備援）
            m = ResistanceRCodeRegex().Match(text);
            if (m.Success)
            {
                string left = m.Groups[1].Value;
                string right = m.Groups[2].Value;
                if (string.IsNullOrEmpty(right))
                {
                    if (TryParseDouble(left, out double whole))
                        return whole;
                }
                else if (TryParseDouble($"{left}.{right}", out double decimalOhm))
                {
                    return decimalOhm;
                }
            }

            m = ResistanceKmRegex().Match(text);
            if (m.Success && TryParseDouble(m.Groups[1].Value, out double val))
            {
                string unit = m.Groups[2].Value;
                return unit switch
                {
                    "K" or "KOHM" => val * 1_000d,
                    "M" or "MOHM" or "MEG" => val * 1_000_000d,
                    _ => null
                };
            }

            return null;
        }

        private static double? TryParseCapacitancePf(string text)
        {
            Match m = CapacitanceRegex().Match(text);
            if (!m.Success || !TryParseDouble(m.Groups[1].Value, out double val))
                return null;

            return m.Groups[2].Value switch
            {
                "PF" => val,
                "NF" => val * 1_000d,
                "UF" or "µF" or "μF" => val * 1_000_000d,
                "MF" => val * 1_000_000_000d,
                _ => null
            };
        }

        private static double? TryParseInductanceNh(string text)
        {
            // 避免誤吃 OHM 尾巴：單位需為獨立 NH/UH/MH/H
            Match m = InductanceRegex().Match(text);
            if (!m.Success || !TryParseDouble(m.Groups[1].Value, out double val))
                return null;

            return m.Groups[2].Value switch
            {
                "NH" => val,
                "UH" => val * 1_000d,
                "MH" => val * 1_000_000d,
                "H" => val * 1_000_000_000d,
                _ => null
            };
        }

        private static double? TryParseFrequencyHz(string text)
        {
            Match m = FrequencyRegex().Match(text);
            if (!m.Success || !TryParseDouble(m.Groups[1].Value, out double val))
                return null;

            return m.Groups[2].Value switch
            {
                "HZ" => val,
                "KHZ" => val * 1_000d,
                "MHZ" => val * 1_000_000d,
                "GHZ" => val * 1_000_000_000d,
                _ => null
            };
        }

        private static double? TryParseCurrentA(string text)
        {
            Match m = CurrentMaRegex().Match(text);
            if (m.Success && TryParseDouble(m.Groups[1].Value, out double ma))
                return ma / 1_000d;

            m = CurrentARegex().Match(text);
            if (m.Success && TryParseDouble(m.Groups[1].Value, out double a))
                return a;

            return null;
        }

        private static double? TryParseVoltageV(string text)
        {
            Match m = VoltageKvRegex().Match(text);
            if (m.Success && TryParseDouble(m.Groups[1].Value, out double kv))
                return kv * 1_000d;

            m = VoltageVRegex().Match(text);
            if (m.Success && TryParseDouble(m.Groups[1].Value, out double v))
                return v;

            return null;
        }

        private static string? TryParsePackage(string text)
        {
            Match m = PackageSizeRegex().Match(text);
            if (m.Success)
                return m.Groups[1].Value.ToUpperInvariant();

            string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string t = token.Trim().ToUpperInvariant();
                if (PackageTokens.Contains(t))
                    return t;

                if (t.StartsWith("SOT", StringComparison.Ordinal) && t.Length <= 8)
                    return t;
            }

            return null;
        }

        private static bool TryParseDouble(string s, out double value) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*OHM\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ResistanceOhmRegex();

        [GeneratedRegex(@"\b(\d+)R(\d*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ResistanceRCodeRegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(KOHM|MOHM|MEG|K|M)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ResistanceKmRegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(PF|NF|UF|MF)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex CapacitanceRegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(NH|UH|MH|H)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex InductanceRegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(GHZ|MHZ|KHZ|HZ)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex FrequencyRegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*MA\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex CurrentMaRegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*A\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex CurrentARegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*KV\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex VoltageKvRegex();

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*V\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex VoltageVRegex();

        [GeneratedRegex(@"\b(0201|0402|0603|0805|1206|1210|1812|2010|2512)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex PackageSizeRegex();
    }
}
