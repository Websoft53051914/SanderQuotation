using Business.DomainModel;
using System.Text.RegularExpressions;

namespace Business.Common
{
    /// <summary>
    /// 補強 AI 未抽出的客戶料號／承認料 token，寫入 LongDesc 類 keyword
    /// </summary>
    public static class SanderModuleItemKeywordSupplement
    {
        private const int MinPartTokenLength = 6;

        /// <summary>整行僅一個 token（如 longdesc 獨立一行的 0080433-005）</summary>
        private static readonly Regex StandaloneLineTokenRegex = new(
            @"^[A-Z0-9][A-Z0-9\-/.]{5,}$",
            RegexOptions.Compiled);

        /// <summary>含連字號的料號樣式（如 0080433-005）</summary>
        private static readonly Regex DashedPartTokenRegex = new(
            @"\b[A-Z0-9]{2,}-[A-Z0-9][A-Z0-9\-/.]{2,}\b",
            RegexOptions.Compiled);

        /// <summary>
        /// 將客戶料號 token 併入 AI 關鍵字清單（LongDesc / LongDesc2）
        /// </summary>
        public static void AppendCustomerPartKeywords(
            IEnumerable<SanderModuleItemDM> items,
            List<TBSanderModuleItemKeywordDM> keywords)
        {
            HashSet<string> existing = keywords
                .Where(x => !string.IsNullOrWhiteSpace(x.No) && !string.IsNullOrWhiteSpace(x.Keyword))
                .Select(x => BuildKey(x.No!, x.Keyword!))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (SanderModuleItemDM item in items)
            {
                if (string.IsNullOrWhiteSpace(item.No))
                    continue;

                AppendFieldTokens(item.No, nameof(SanderModuleItemDM.LongDesc), item.LongDesc, existing, keywords);
                AppendFieldTokens(item.No, nameof(SanderModuleItemDM.LongDesc2), item.LongDesc2, existing, keywords);
                AppendFieldTokens(item.No, nameof(SanderModuleItemDM.Description2), item.Description2, existing, keywords);
            }
        }

        private static void AppendFieldTokens(
            string no,
            string columnName,
            string? fieldText,
            HashSet<string> existing,
            List<TBSanderModuleItemKeywordDM> keywords)
        {
            if (string.IsNullOrWhiteSpace(fieldText))
                return;

            string normalizedField = SandermoduleItemNormalizer.Normalize(fieldText);
            if (string.IsNullOrWhiteSpace(normalizedField))
                return;

            foreach (string token in ExtractCustomerPartTokens(normalizedField))
            {
                string key = BuildKey(no, token);
                if (!existing.Add(key))
                    continue;

                keywords.Add(new TBSanderModuleItemKeywordDM
                {
                    No = no,
                    ColumnName = columnName,
                    Keyword = token,
                });
            }
        }

        /// <summary>
        /// 從已正規化文字抽出客戶料號 token
        /// </summary>
        public static IEnumerable<string> ExtractCustomerPartTokens(string normalizedText)
        {
            HashSet<string> tokens = new(StringComparer.OrdinalIgnoreCase);

            foreach (string line in normalizedText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                string lineToken = line.Trim();
                if (IsCustomerPartToken(lineToken))
                    tokens.Add(lineToken);
            }

            foreach (Match match in DashedPartTokenRegex.Matches(normalizedText))
            {
                string token = match.Value;
                if (IsCustomerPartToken(token))
                    tokens.Add(token);
            }

            return tokens;
        }

        private static bool IsCustomerPartToken(string token)
        {
            if (token.Length < MinPartTokenLength)
                return false;

            if (!token.Any(char.IsDigit))
                return false;

            if (!StandaloneLineTokenRegex.IsMatch(token))
                return false;

            return true;
        }

        private static string BuildKey(string no, string keyword) => $"{no}|{keyword}";
    }
}
