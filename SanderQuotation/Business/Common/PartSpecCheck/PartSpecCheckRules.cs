namespace Business.Common.PartSpecCheck
{
    /// <summary>
    /// 料品檢查參數表（對應 Excel：Item_Category_Code／主參數／硬條件）
    /// </summary>
    public static class PartSpecCheckRules
    {
        private static readonly PartSpecCheckRule[] Rules =
        [
            new PartSpecCheckRule
            {
                ItemCategoryCode = "RESISTOR",
                Aliases = ["RESISTOR"],
                MainParams = [PartSpecParamCodes.Resistance],
                HardParams = [PartSpecParamCodes.Package]
            },
            new PartSpecCheckRule
            {
                ItemCategoryCode = "CAPACITORS",
                Aliases = ["CAPACITORS", "CAPACITOR"],
                MainParams = [PartSpecParamCodes.Capacitance],
                HardParams = [PartSpecParamCodes.Voltage, PartSpecParamCodes.Package]
            },
            new PartSpecCheckRule
            {
                ItemCategoryCode = "INDUCTOR",
                Aliases = ["INDUCTOR"],
                MainParams = [PartSpecParamCodes.Inductance],
                HardParams = []
            },
            new PartSpecCheckRule
            {
                ItemCategoryCode = "OSCILLATOR",
                Aliases = ["OSCILLATOR"],
                MainParams = [PartSpecParamCodes.Frequency],
                HardParams = []
            },
            new PartSpecCheckRule
            {
                ItemCategoryCode = "FUSE",
                Aliases = ["FUSE"],
                MainParams = [PartSpecParamCodes.RatedCurrent],
                HardParams = [PartSpecParamCodes.Voltage]
            }
        ];

        /// <summary>
        /// 不做規格比對之類型（維持向量 Top1）
        /// </summary>
        private static readonly HashSet<string> SkipCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "IC", "LED", "CONNECTOR", "DIODE", "MODULE", "WIRE", "OTHER"
        };

        private static readonly Dictionary<string, PartSpecCheckRule> AliasToRule =
            Rules
                .SelectMany(r => r.Aliases.Select(a => (Alias: a.ToUpperInvariant(), Rule: r)))
                .ToDictionary(x => x.Alias, x => x.Rule, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 依類型代碼取得規則；不做規格比對或未列類型回傳 null
        /// </summary>
        public static PartSpecCheckRule? GetRule(string? categoryCode)
        {
            if (string.IsNullOrWhiteSpace(categoryCode))
                return null;

            string key = categoryCode.Trim().ToUpperInvariant();
            if (SkipCategories.Contains(key))
                return null;

            return AliasToRule.TryGetValue(key, out PartSpecCheckRule? rule) ? rule : null;
        }

        /// <summary>
        /// 兩類型是否視為同一檢查類別（含別名）
        /// </summary>
        public static bool IsSameCategory(string? categoryA, string? categoryB)
        {
            PartSpecCheckRule? ruleA = GetRule(categoryA);
            PartSpecCheckRule? ruleB = GetRule(categoryB);
            if (ruleA != null && ruleB != null)
                return string.Equals(ruleA.ItemCategoryCode, ruleB.ItemCategoryCode, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(categoryA) || string.IsNullOrWhiteSpace(categoryB))
                return false;

            return string.Equals(categoryA.Trim(), categoryB.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static string ParamDisplayName(string paramCode) => paramCode switch
        {
            PartSpecParamCodes.Resistance => "阻值",
            PartSpecParamCodes.Capacitance => "容值",
            PartSpecParamCodes.Inductance => "感值",
            PartSpecParamCodes.Frequency => "頻率",
            PartSpecParamCodes.RatedCurrent => "額定電流",
            PartSpecParamCodes.Voltage => "電壓",
            PartSpecParamCodes.Package => "封裝",
            _ => paramCode
        };
    }
}
