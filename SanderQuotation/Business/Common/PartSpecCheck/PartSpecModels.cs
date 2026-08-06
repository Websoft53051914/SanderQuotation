namespace Business.Common.PartSpecCheck
{
    /// <summary>
    /// 料品檢查參數：主參數／硬條件欄位代碼
    /// </summary>
    public static class PartSpecParamCodes
    {
        public const string Resistance = "Resistance";
        public const string Capacitance = "Capacitance";
        public const string Inductance = "Inductance";
        public const string Frequency = "Frequency";
        public const string RatedCurrent = "RatedCurrent";
        public const string Voltage = "Voltage";
        public const string Package = "Package";
    }

    /// <summary>
    /// 單一類型的檢查規則（對應料品檢查參數表）
    /// </summary>
    public sealed class PartSpecCheckRule
    {
        public required string ItemCategoryCode { get; init; }

        /// <summary>同類別名（如 CAPACITOR ↔ CAPACITORS）</summary>
        public required IReadOnlyList<string> Aliases { get; init; }

        /// <summary>主參數：兩邊必須抽出且相等</summary>
        public required IReadOnlyList<string> MainParams { get; init; }

        /// <summary>硬條件：僅 BOM／查詢端有寫時才比</summary>
        public required IReadOnlyList<string> HardParams { get; init; }
    }

    /// <summary>
    /// 自標準化 Keyword 解析出的規格
    /// </summary>
    public sealed class ParsedPartSpec
    {
        public string? CategoryCode { get; set; }

        public string RawKeyword { get; set; } = string.Empty;

        /// <summary>阻值（Ohm）</summary>
        public double? ResistanceOhm { get; set; }

        /// <summary>容值（pF）</summary>
        public double? CapacitancePf { get; set; }

        /// <summary>感值（nH）</summary>
        public double? InductanceNh { get; set; }

        /// <summary>頻率（Hz）</summary>
        public double? FrequencyHz { get; set; }

        /// <summary>額定電流（A）</summary>
        public double? CurrentA { get; set; }

        /// <summary>電壓／耐壓／額定電壓（V）</summary>
        public double? VoltageV { get; set; }

        /// <summary>封裝（正規化大寫）</summary>
        public string? Package { get; set; }

        public bool HasParam(string paramCode) => GetParamValue(paramCode) != null;

        public object? GetParamValue(string paramCode) => paramCode switch
        {
            PartSpecParamCodes.Resistance => ResistanceOhm.HasValue ? ResistanceOhm : null,
            PartSpecParamCodes.Capacitance => CapacitancePf.HasValue ? CapacitancePf : null,
            PartSpecParamCodes.Inductance => InductanceNh.HasValue ? InductanceNh : null,
            PartSpecParamCodes.Frequency => FrequencyHz.HasValue ? FrequencyHz : null,
            PartSpecParamCodes.RatedCurrent => CurrentA.HasValue ? CurrentA : null,
            PartSpecParamCodes.Voltage => VoltageV.HasValue ? VoltageV : null,
            PartSpecParamCodes.Package => string.IsNullOrWhiteSpace(Package) ? null : Package,
            _ => null
        };

        public string FormatParam(string paramCode) => paramCode switch
        {
            PartSpecParamCodes.Resistance => ResistanceOhm.HasValue ? $"{ResistanceOhm}OHM" : "(無)",
            PartSpecParamCodes.Capacitance => CapacitancePf.HasValue ? $"{CapacitancePf}PF" : "(無)",
            PartSpecParamCodes.Inductance => InductanceNh.HasValue ? $"{InductanceNh}NH" : "(無)",
            PartSpecParamCodes.Frequency => FrequencyHz.HasValue ? $"{FrequencyHz}HZ" : "(無)",
            PartSpecParamCodes.RatedCurrent => CurrentA.HasValue ? $"{CurrentA}A" : "(無)",
            PartSpecParamCodes.Voltage => VoltageV.HasValue ? $"{VoltageV}V" : "(無)",
            PartSpecParamCodes.Package => string.IsNullOrWhiteSpace(Package) ? "(無)" : Package,
            _ => "(無)"
        };
    }

    /// <summary>
    /// 單筆候選硬比結果
    /// </summary>
    public sealed class PartSpecMatchResult
    {
        public bool Passed { get; init; }
        public string Reason { get; init; } = string.Empty;

        public static PartSpecMatchResult Pass(string reason = "硬比通過") =>
            new() { Passed = true, Reason = reason };

        public static PartSpecMatchResult Fail(string reason) =>
            new() { Passed = false, Reason = reason };
    }
}
