using System.Text.Json;

namespace backend.Models
{
    /// <summary>
    /// 配方主檔資料模型（對應 TB_Recipe）
    /// </summary>
    public class RecipeData
    {
        public long Id { get; set; }
        public string RecipeNo { get; set; } = "";
        public string RecipeName { get; set; } = "";
        public string CurrentVersionNo { get; set; } = "";
        public string Engineer { get; set; } = "";
        public string RecipeContent { get; set; } = "";
        public long Creator { get; set; }
        public DateTime CreateTime { get; set; }
        public long Updater { get; set; }
        public DateTime UpdateTime { get; set; }
        public int Status { get; set; }
        /// <summary>關聯的版號規則 ID（0 = 無規則，手動設定）</summary>
        public long VersionNoRuleId { get; set; }
    }

    /// <summary>
    /// 配方版本資料模型（對應 TB_RecipeVersion）
    /// </summary>
    public class RecipeVersionData
    {
        public long Id { get; set; }
        public long RecipeId { get; set; }
        public string VersionNo { get; set; } = "";
        public string VersionLog { get; set; } = "";
        public string RecipeContent { get; set; } = "";
        public bool IsCurrent { get; set; }
        /// <summary>版號種子 JSON（例：{"MAJOR":1,"MINOR":2,"PATCH":3,"SEQ":0}），用於計算下一版號</summary>
        public string VersionSeed { get; set; } = "";
        public string VersionBumpType { get; set; } = "";
        public long Creator { get; set; }
        public DateTime CreateTime { get; set; }
        public long Updater { get; set; }
        public DateTime UpdateTime { get; set; }
        public int Status { get; set; }
    }

    /// <summary>
    /// 版號規則資料模型（對應 TB_RecipeVersionNoRule）
    /// </summary>
    public class RecipeVersionNoRuleData
    {
        public long Id { get; set; }
        public string RuleName { get; set; } = "";
        public string RuleSetting { get; set; } = "";
        public long Creator { get; set; }
        public DateTime CreateTime { get; set; }
        public long Updater { get; set; }
        public DateTime UpdateTime { get; set; }
        public int Status { get; set; }
    }

    internal class DemoDbRoot
    {
        public List<RecipeData> Recipes { get; set; } = new();
        public List<RecipeVersionData> RecipeVersions { get; set; } = new();
        public List<RecipeVersionNoRuleData> RecipeVersionNoRules { get; set; } = new();
    }

    /// <summary>
    /// 以 demo.json 模擬資料庫的 CRUD Helper
    /// </summary>
    public class RecipeJsonHelper
    {
        private static readonly string _filePath;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        static RecipeJsonHelper()
        {
            // 定位到執行檔所在目錄
            string baseDir = AppContext.BaseDirectory;
            _filePath = Path.Combine(baseDir, "demo.json");

            // 若執行目錄沒有 demo.json，往上找專案根目錄（開發時使用）
            if (!File.Exists(_filePath))
            {
                DirectoryInfo? dir = new DirectoryInfo(baseDir);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, "demo.json");
                    if (File.Exists(candidate))
                    {
                        _filePath = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }
            }
        }

        #region -- 私有 IO --

        /// <summary>從 JSON 檔讀取資料庫快照。</summary>
        private static DemoDbRoot Load()
        {
            if (!File.Exists(_filePath))
                return new DemoDbRoot();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<DemoDbRoot>(json, _jsonOpts) ?? new DemoDbRoot();
        }

        /// <summary>將資料庫快照寫入 JSON 檔。</summary>
        private static void Save(DemoDbRoot db)
        {
            string json = JsonSerializer.Serialize(db, _jsonOpts);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>計算清單中的下一個可用 ID。</summary>
        private static long NextId<T>(IEnumerable<T> list, Func<T, long> getId)
        {
            return list.Any() ? list.Max(getId) + 1 : 1;
        }

        #endregion -- 私有 IO --

        #region -- 配方主檔 CRUD --

        /// <summary>取得配方清單（過濾已廢止紀錄）。</summary>
        /// <param name="keyword">模糊比對編號/名稱/工程師，null 時回傳全部</param>
        public static List<RecipeData> GetRecipeList(string? keyword = null)
        {
            DemoDbRoot db = Load();
            // 只返回未作廢的記錄
            IEnumerable<RecipeData> list = db.Recipes.Where(r => r.Status != 9).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                list = list.Where(r =>
                    r.RecipeNo.ToLower().Contains(keyword) ||
                    r.RecipeName.ToLower().Contains(keyword) ||
                    r.Engineer.ToLower().Contains(keyword));
            }

            return list.OrderBy(r => r.RecipeNo).ToList();
        }

        /// <summary>取得指定配方的版本數量。</summary>
        public static int GetVersionCount(long recipeId)
        {
            return Load().RecipeVersions.Count(v => v.RecipeId == recipeId);
        }

        /// <summary>依 Id 取得單筆配方；找不到時回傳 null。</summary>
        public static RecipeData? GetRecipeById(long id)
        {
            return Load().Recipes.FirstOrDefault(r => r.Id == id);
        }

        /// <summary>依 RecipeNo 取得單筆配方（不含已廢止）；找不到時回傳 null。</summary>
        public static RecipeData? GetRecipeByRecipeNo(string recipeNo)
        {
            return Load().Recipes.FirstOrDefault(r =>
                r.Status != 9 &&
                string.Equals(r.RecipeNo, recipeNo, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>新增配方，自動補 Id 與 CreateTime/UpdateTime。</summary>
        public static RecipeData CreateRecipe(RecipeData recipe)
        {
            DemoDbRoot db = Load();
            recipe.Id = NextId(db.Recipes, r => r.Id);
            recipe.CreateTime = DateTime.Now;
            recipe.UpdateTime = DateTime.Now;
            db.Recipes.Add(recipe);
            Save(db);
            return recipe;
        }

        /// <summary>更新配方。保留原始 Creator / CreateTime / VersionNoRuleId，更新 UpdateTime。</summary>
        /// <returns>true：成功；false：找不到指定 Id。</returns>
        public static bool UpdateRecipe(RecipeData recipe)
        {
            DemoDbRoot db = Load();
            int idx = db.Recipes.FindIndex(r => r.Id == recipe.Id);
            if (idx < 0) return false;

            recipe.CreateTime = db.Recipes[idx].CreateTime;
            recipe.Creator = db.Recipes[idx].Creator;
            recipe.VersionNoRuleId = db.Recipes[idx].VersionNoRuleId; // 不允许编辑时修改版号规则
            recipe.UpdateTime = DateTime.Now;
            db.Recipes[idx] = recipe;
            Save(db);
            return true;
        }

        /// <summary>軟刪除配方（Status 設為 9）。</summary>
        /// <returns>true：成功；false：找不到指定 Id。</returns>
        public static bool DeleteRecipe(long id)
        {
            DemoDbRoot db = Load();
            RecipeData? recipe = db.Recipes.FirstOrDefault(r => r.Id == id);
            if (recipe == null) return false;

            // 軟刪除：將 Status 設為 9
            recipe.Status = 9;
            recipe.UpdateTime = DateTime.Now;
            Save(db);
            return true;
        }

        /// <summary>批次軟刪除配方。</summary>
        /// <returns>true：至少刪除一筆；false：全部找不到。</returns>
        public static bool BatchDeleteRecipe(IEnumerable<long> ids)
        {
            DemoDbRoot db = Load();
            HashSet<long> idSet = ids.ToHashSet();
            List<RecipeData> targets = db.Recipes.Where(r => idSet.Contains(r.Id)).ToList();
            if (!targets.Any()) return false;

            foreach (RecipeData r in targets)
            {
                r.Status = 9;
                r.UpdateTime = DateTime.Now;
            }
            Save(db);
            return true;
        }

        #endregion -- 配方主檔 CRUD --

        #region -- 版本 CRUD --

        /// <summary>取得指定配方的版本清單（依建立時間逆序排列）。</summary>
        public static List<RecipeVersionData> GetVersionList(long recipeId)
        {
            return Load().RecipeVersions
                .Where(v => v.RecipeId == recipeId)
                .OrderByDescending(v => v.CreateTime)
                .ToList();
        }

        /// <summary>依 Id 取得單筆版本；找不到時回傳 null。</summary>
        public static RecipeVersionData? GetVersionById(long id)
        {
            return Load().RecipeVersions.FirstOrDefault(v => v.Id == id);
        }

        /// <summary>新增版本。若 IsCurrent=true，同步修改擺主檔現行版號。</summary>
        public static RecipeVersionData CreateVersion(RecipeVersionData version)
        {
            DemoDbRoot db = Load();
            version.Id = NextId(db.RecipeVersions, v => v.Id);
            version.CreateTime = DateTime.Now;
            version.UpdateTime = DateTime.Now;

            // 將同 RecipeId 其他版本設為非現行
            if (version.IsCurrent)
            {
                foreach (RecipeVersionData v in db.RecipeVersions.Where(v => v.RecipeId == version.RecipeId))
                    v.IsCurrent = false;

                // 同步更新主檔現行版號
                RecipeData? recipe = db.Recipes.FirstOrDefault(r => r.Id == version.RecipeId);
                if (recipe != null)
                {
                    recipe.CurrentVersionNo = version.VersionNo;
                    recipe.UpdateTime = DateTime.Now;
                }
            }

            db.RecipeVersions.Add(version);
            Save(db);
            return version;
        }

        /// <summary>將指定版本設為現行版本，同步更新配方主檔 CurrentVersionNo。</summary>
        /// <returns>true：成功；false：找不到版本。</returns>
        public static bool SetCurrentVersion(long versionId)
        {
            DemoDbRoot db = Load();
            RecipeVersionData? ver = db.RecipeVersions.FirstOrDefault(v => v.Id == versionId);
            if (ver == null) return false;

            foreach (RecipeVersionData v in db.RecipeVersions.Where(v => v.RecipeId == ver.RecipeId))
                v.IsCurrent = false;

            ver.IsCurrent = true;

            RecipeData? recipe = db.Recipes.FirstOrDefault(r => r.Id == ver.RecipeId);
            if (recipe != null)
            {
                recipe.CurrentVersionNo = ver.VersionNo;
                recipe.UpdateTime = DateTime.Now;
            }

            Save(db);
            return true;
        }

        /// <summary>取得指定配方最新版本（優先取 IsCurrent=true，否則依 CreateTime 逆序取第一筆）。</summary>
        public static RecipeVersionData? GetCurrentOrLatestVersion(long recipeId)
        {
            List<RecipeVersionData> versions = Load().RecipeVersions
                .Where(v => v.RecipeId == recipeId)
                .ToList();
            return versions.FirstOrDefault(v => v.IsCurrent)
                ?? versions.OrderByDescending(v => v.CreateTime).FirstOrDefault();
        }

        #endregion -- 版本 CRUD --

        #region -- 版號規則 CRUD --

        /// <summary>
        /// 取得版號規則清單（過濾已廢止紀錄）。
        /// </summary>
        /// <param name="keyword">模糊比對規則名稱，null 時回傳全部</param>
        public static List<RecipeVersionNoRuleData> GetVersionNoRuleList(string? keyword = null)
        {
            DemoDbRoot db = Load();
            IEnumerable<RecipeVersionNoRuleData> list = db.RecipeVersionNoRules.Where(r => r.Status != 9).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                list = list.Where(r => r.RuleName.ToLower().Contains(keyword));
            }

            return list.OrderBy(r => r.RuleName).ToList();
        }

        /// <summary>依 Id 取得單筆版號規則；找不到時回傳 null。</summary>
        public static RecipeVersionNoRuleData? GetVersionNoRuleById(long id)
            => Load().RecipeVersionNoRules.FirstOrDefault(r => r.Id == id);

        /// <summary>新增版號規則，自動補 Id 與 CreateTime/UpdateTime。</summary>
        public static RecipeVersionNoRuleData CreateVersionNoRule(RecipeVersionNoRuleData rule)
        {
            DemoDbRoot db = Load();
            rule.Id = NextId(db.RecipeVersionNoRules, r => r.Id);
            rule.CreateTime = DateTime.Now;
            rule.UpdateTime = DateTime.Now;
            db.RecipeVersionNoRules.Add(rule);
            Save(db);
            return rule;
        }

        /// <summary>
        /// 更新版號規則。保留原始 Creator / CreateTime，更新 UpdateTime。
        /// </summary>
        /// <returns>true：成功；false：找不到指定 Id。</returns>
        public static bool UpdateVersionNoRule(RecipeVersionNoRuleData rule)
        {
            DemoDbRoot db = Load();
            int idx = db.RecipeVersionNoRules.FindIndex(r => r.Id == rule.Id);
            if (idx < 0) return false;

            // 保留不可覆寫的欄位
            rule.CreateTime = db.RecipeVersionNoRules[idx].CreateTime;
            rule.Creator = db.RecipeVersionNoRules[idx].Creator;
            rule.UpdateTime = DateTime.Now;

            db.RecipeVersionNoRules[idx] = rule;
            Save(db);
            return true;
        }

        /// <summary>軟刪除版號規則（Status 設為 9）。</summary>
        /// <returns>true：成功；false：找不到指定 Id。</returns>
        public static bool DeleteVersionNoRule(long id)
        {
            DemoDbRoot db = Load();
            RecipeVersionNoRuleData? rule = db.RecipeVersionNoRules.FirstOrDefault(r => r.Id == id);
            if (rule == null) return false;

            rule.Status = 9;
            rule.UpdateTime = DateTime.Now;
            Save(db);
            return true;
        }

        #endregion -- 版號規則 CRUD --
    }
}
