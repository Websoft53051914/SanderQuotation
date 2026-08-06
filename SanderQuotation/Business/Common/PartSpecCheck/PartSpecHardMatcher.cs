namespace Business.Common.PartSpecCheck
{
    /// <summary>
    /// Step3 料品規格硬比：主參數必相等；硬條件僅查詢端有寫時才比
    /// </summary>
    public static class PartSpecHardMatcher
    {
        private const double RelativeTolerance = 1e-6;
        private const double AbsoluteTolerance = 1e-9;

        /// <summary>
        /// 評估候選是否通過規則
        /// </summary>
        /// <param name="query">查詢端規格</param>
        /// <param name="candidate">候選 Keyword 規格</param>
        /// <param name="rule">檢查規則</param>
        /// <param name="candidateItemCategoryCode">候選主檔 ItemCategoryCode（可選）</param>
        public static PartSpecMatchResult Evaluate(
            ParsedPartSpec query,
            ParsedPartSpec candidate,
            PartSpecCheckRule rule,
            string? candidateItemCategoryCode = null)
        {
            if (!string.IsNullOrWhiteSpace(candidateItemCategoryCode))
                PartSpecParser.ApplyItemCategoryCode(candidate, candidateItemCategoryCode);

            string? queryCat = query.CategoryCode;
            string? candCat = candidate.CategoryCode;

            if (!string.IsNullOrWhiteSpace(candCat)
                && !string.IsNullOrWhiteSpace(queryCat)
                && !PartSpecCheckRules.IsSameCategory(queryCat, candCat))
            {
                return PartSpecMatchResult.Fail($"類型不符：查詢={queryCat}，候選={candCat}");
            }

            foreach (string mainParam in rule.MainParams)
            {
                if (!query.HasParam(mainParam))
                    return PartSpecMatchResult.Fail($"查詢端缺少主參數「{PartSpecCheckRules.ParamDisplayName(mainParam)}」");

                if (!candidate.HasParam(mainParam))
                    return PartSpecMatchResult.Fail($"候選缺少主參數「{PartSpecCheckRules.ParamDisplayName(mainParam)}」");

                if (!ParamsEqual(query, candidate, mainParam))
                {
                    return PartSpecMatchResult.Fail(
                        $"主參數「{PartSpecCheckRules.ParamDisplayName(mainParam)}」不符：查詢={query.FormatParam(mainParam)}，候選={candidate.FormatParam(mainParam)}");
                }
            }

            foreach (string hardParam in rule.HardParams)
            {
                if (!query.HasParam(hardParam))
                    continue;

                if (!candidate.HasParam(hardParam))
                {
                    return PartSpecMatchResult.Fail(
                        $"硬條件「{PartSpecCheckRules.ParamDisplayName(hardParam)}」：查詢有寫 {query.FormatParam(hardParam)}，候選未抽出");
                }

                if (!ParamsEqual(query, candidate, hardParam))
                {
                    return PartSpecMatchResult.Fail(
                        $"硬條件「{PartSpecCheckRules.ParamDisplayName(hardParam)}」不符：查詢={query.FormatParam(hardParam)}，候選={candidate.FormatParam(hardParam)}");
                }
            }

            List<string> passed = [];
            foreach (string p in rule.MainParams)
                passed.Add($"{PartSpecCheckRules.ParamDisplayName(p)}={query.FormatParam(p)}");
            foreach (string p in rule.HardParams.Where(query.HasParam))
                passed.Add($"{PartSpecCheckRules.ParamDisplayName(p)}={query.FormatParam(p)}");

            return PartSpecMatchResult.Pass("硬比通過：" + string.Join(", ", passed));
        }

        /// <summary>
        /// 查詢端是否具備該規則全部主參數（否則 Step3 應直接 Miss）
        /// </summary>
        public static bool QueryHasAllMainParams(ParsedPartSpec query, PartSpecCheckRule rule) =>
            rule.MainParams.All(query.HasParam);

        private static bool ParamsEqual(ParsedPartSpec query, ParsedPartSpec candidate, string paramCode)
        {
            if (paramCode == PartSpecParamCodes.Package)
            {
                return string.Equals(
                    query.Package?.Trim(),
                    candidate.Package?.Trim(),
                    StringComparison.OrdinalIgnoreCase);
            }

            object? q = query.GetParamValue(paramCode);
            object? c = candidate.GetParamValue(paramCode);
            if (q is double qd && c is double cd)
                return NearlyEqual(qd, cd);

            return Equals(q, c);
        }

        private static bool NearlyEqual(double a, double b)
        {
            double diff = Math.Abs(a - b);
            if (diff <= AbsoluteTolerance)
                return true;
            double scale = Math.Max(Math.Abs(a), Math.Abs(b));
            return diff <= scale * RelativeTolerance;
        }
    }
}
