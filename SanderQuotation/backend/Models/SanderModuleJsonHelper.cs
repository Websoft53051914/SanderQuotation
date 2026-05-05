using System.Text.Json;

namespace backend.Models
{
    /// <summary>
    /// Sander 採購型號主檔資料模型（對應 sandermodule_item）
    /// </summary>
    public class SanderModuleItemData
    {
        public long Id { get; set; }

        /// <summary>料號</summary>
        public string No { get; set; } = "";

        /// <summary>品名</summary>
        public string Description { get; set; } = "";

        /// <summary>品名2</summary>
        public string Description2 { get; set; } = "";

        /// <summary>長描述</summary>
        public string LongDesc { get; set; } = "";

        /// <summary>長描述2</summary>
        public string LongDesc2 { get; set; } = "";
    }

    internal class SanderModuleDbRoot
    {
        public List<SanderModuleItemData> Items { get; set; } = new();
    }

    /// <summary>
    /// 以 sandermodule.json 模擬 sandermodule_item 資料表的查詢 Helper
    /// </summary>
    public class SanderModuleJsonHelper
    {
        private static readonly string _filePath;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        static SanderModuleJsonHelper()
        {
            string baseDir = AppContext.BaseDirectory;
            _filePath = Path.Combine(baseDir, "sandermodule.json");

            if (!File.Exists(_filePath))
            {
                DirectoryInfo? dir = new DirectoryInfo(baseDir);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, "sandermodule.json");
                    if (File.Exists(candidate))
                    {
                        _filePath = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }
            }
        }

        /// <summary>從 JSON 檔讀取資料。</summary>
        private static SanderModuleDbRoot Load()
        {
            if (!File.Exists(_filePath))
                return new SanderModuleDbRoot();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<SanderModuleDbRoot>(json, _jsonOpts) ?? new SanderModuleDbRoot();
        }

        /// <summary>取得採購型號清單（支援關鍵字搜尋）</summary>
        public static List<SanderModuleItemData> GetList(string? keyword = null)
        {
            SanderModuleDbRoot db = Load();
            List<SanderModuleItemData> list = db.Items;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                list = list.Where(i =>
                    i.No.ToLower().Contains(kw) ||
                    i.Description.ToLower().Contains(kw) ||
                    i.Description2.ToLower().Contains(kw) ||
                    i.LongDesc.ToLower().Contains(kw)
                ).ToList();
            }

            return list.OrderBy(i => i.No).ToList();
        }
    }
}
