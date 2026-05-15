using System.Text;
using System.Text.RegularExpressions;

namespace Business.Common
{
    public static class SandermoduleItemNormalizer
    {
        // 雜訊字典
        private static readonly string[] NoiseWords = new[]
        {
            "ROHS", "PB FREE", "LEAD FREE", "LF",
            "REEL", "T/R", "TAPE", "CUT TAPE",
            "BULK", "PACK", "PACKING"
        };

        // 關鍵字標準化
        private static readonly Dictionary<string, string> KeywordMap = new()
        {
            { "RES", "RESISTOR" },
            { "R", "RESISTOR" },
            { "CAP", "CAPACITOR" },
            { "C", "CAPACITOR" },
            { "DIO", "DIODE" },
            { "D", "DIODE" }
        };

        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 1️ 全形轉半形
            var normalized = ToHalfWidth(input);

            // 2️ 大寫
            normalized = normalized.ToUpperInvariant();

            // 3️ 移除特殊符號（保留 . % / -）
            //normalized = Regex.Replace(normalized, @"[^A-Z0-9\.\%\-/\s]", " ");

            // 4️ 移除雜訊字
            //foreach (var noise in NoiseWords)
            //{
            //    normalized = normalized.Replace(noise, " ");
            //}

            // 5️ 單位標準化
            //normalized = NormalizeUnits(normalized);

            // 6️ 多空白合一
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            // 7️ keyword mapping
            //normalized = NormalizeKeywords(normalized);

            return normalized;
        }

        public static List<string> Tokenize(string normalized)
        {
            return normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        // 🔧 單位處理
        private static string NormalizeUnits(string input)
        {
            // 電阻
            input = Regex.Replace(input, @"(\d+(\.\d+)?)\s*(K|OHM|Ω)", "$1KΩ");
            input = Regex.Replace(input, @"(\d+(\.\d+)?)\s*(MΩ|MEG)", "$1MΩ");

            // 電容
            input = Regex.Replace(input, @"(\d+(\.\d+)?)\s*(µ|μ|u)F", "$1UF");
            // nF / pF（通常已經一致，但保險）
            input = Regex.Replace(input, @"(\d+(\.\d+)?)\s*NF", "$1NF");
            input = Regex.Replace(input, @"(\d+(\.\d+)?)\s*PF", "$1PF");

            // 電壓
            input = Regex.Replace(input, @"(\d+)\s*V", "$1V");

            // 功率
            input = Regex.Replace(input, @"(\d+(\.\d+)?)\s*W", "$1W");

            return input;
        }

        // 🔧 keyword mapping
        private static string NormalizeKeywords(string input)
        {
            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length; i++)
            {
                if (KeywordMap.TryGetValue(tokens[i], out var mapped))
                {
                    tokens[i] = mapped;
                }
            }

            return string.Join(" ", tokens);
        }

        // 🔧 全形轉半形
        private static string ToHalfWidth(string input)
        {
            return input.Normalize(NormalizationForm.FormKC);
        }
    }
}
