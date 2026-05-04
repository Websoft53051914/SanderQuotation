using CommonClass.Models;
using Core.Utility.Web.Base;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using ViewModel.RecipeMgmt;

namespace backend.Controllers
{
    /// <summary>配方主檔 API Controller</summary>
    [Route("api/RecipeMgmt")]
    public class RecipeMgmtController : ApiBaseController
    {
        #region -- 狀態對照 --

        private static readonly Dictionary<int, (string Name, string BadgeCss)> StatusMap = new()
        {
            { 0, ("草稿",   "badge bg-warning-subtle text-warning") },
            { 1, ("已發行", "badge bg-success-subtle text-success") },
            { 2, ("審核中", "badge bg-info-subtle text-info") },
            { 9, ("已廢止", "badge bg-secondary-subtle text-secondary") },
        };

        private static readonly Dictionary<string, string> SeedCssMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "MAJOR", "badge bg-danger-subtle text-danger" },
            { "MINOR", "badge bg-primary-subtle text-primary" },
            { "PATCH", "badge bg-secondary-subtle text-secondary" },
        };

        private static readonly Dictionary<string, string> BumpTypeCssMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "MAJOR", "badge bg-danger-subtle text-danger" },
            { "MINOR", "badge bg-primary-subtle text-primary" },
            { "PATCH", "badge bg-secondary-subtle text-secondary" },
        };

        /// <summary>依使用者 ID 取得顯示名稱</summary>
        private static string GetUserName(long userId) => userId switch
        {
            1 => "系統管理員",
            _ => $"User#{userId}"
        };

        /// <summary>將 RecipeData 對應至清單 ViewModel。</summary>
        private static RecipeGridVM MapToGridVM(RecipeData r, int pVersionCount, string ruleName = "", string currentVersionNo = "")
        {
            RecipeGridVM vm = new();
            vm.Id = r.Id;
            vm.RecipeNo = r.RecipeNo;
            vm.RecipeName = r.RecipeName;
            vm.CurrentVersionNo = currentVersionNo;
            vm.Engineer = r.Engineer;
            vm.CreateTime = r.CreateTime.ToString("yyyy/MM/dd");
            vm.UpdateTime = r.UpdateTime.ToString("yyyy/MM/dd");
            vm.VersionCount = pVersionCount;
            vm.VersionNoRuleName = ruleName;
            return vm;
        }

        /// <summary>將 RecipeData 對應至編輯 ViewModel。</summary>
        private static RecipeEditVM MapToEditVM(RecipeData r)
        {
            RecipeEditVM vm = new();
            vm.Id = r.Id;
            vm.RecipeNo = r.RecipeNo;
            vm.RecipeName = r.RecipeName;
            vm.CurrentVersionNo = RecipeJsonHelper.GetCurrentOrLatestVersion(r.Id)?.VersionNo ?? "";
            vm.Engineer = r.Engineer;
            vm.RecipeContent = r.RecipeContent;
            vm.CreateTime = r.CreateTime.ToString("yyyy/MM/dd HH:mm");
            vm.UpdateTime = r.UpdateTime.ToString("yyyy/MM/dd HH:mm");
            vm.Creator = r.Creator;
            vm.Updater = r.Updater;
            vm.StatusCode = r.Status;
            vm.StatusName = StatusMap.TryGetValue(r.Status, out (string Name, string BadgeCss) s) ? s.Name : r.Status.ToString();
            vm.VersionNoRuleId = r.VersionNoRuleId;
            if (r.VersionNoRuleId > 0)
            {
                RecipeVersionNoRuleData? rule = RecipeJsonHelper.GetVersionNoRuleById(r.VersionNoRuleId);
                vm.VersionNoRuleName = rule?.RuleName ?? "已删除的規則";
            }
            return vm;
        }

        /// <summary>將 RecipeVersionData 對應至版本 ViewModel。</summary>
        private static RecipeVersionVM MapToVersionVM(RecipeVersionData v)
        {
            RecipeVersionVM vm = new();
            vm.Id = v.Id;
            vm.RecipeId = v.RecipeId;
            vm.VersionNo = v.VersionNo;
            vm.VersionLog = v.VersionLog;
            vm.RecipeContent = v.RecipeContent;
            vm.IsCurrent = v.IsCurrent;
            vm.VersionSeed = v.VersionBumpType;  // VersionSeed 現儲存 JSON，以 BumpType 呈現標籤
            vm.VersionSeedCss = SeedCssMap.TryGetValue(v.VersionBumpType, out string? css) ? css : "badge bg-secondary";
            vm.VersionBumpType = v.VersionBumpType;
            vm.VersionBumpTypeCss = BumpTypeCssMap.TryGetValue(v.VersionBumpType, out string? bCss) ? bCss : "badge bg-secondary-subtle text-secondary";
            vm.CreateTime = v.CreateTime.ToString("yyyy/MM/dd HH:mm");
            vm.CreatorName = GetUserName(v.Creator);
            vm.UpdaterName = GetUserName(v.Updater);
            return vm;
        }

        #endregion -- 狀態對照 --

        #region -- 查詢 --

        /// <summary>取得配方清單（支援關鍵字搜尋）</summary>
        [HttpGet("GetList")]
        public ActionResult GetList(string? keyword = null)
        {
            try
            {
                List<RecipeData> recipes = RecipeJsonHelper.GetRecipeList(keyword);
                Dictionary<long, string> ruleNameMap = RecipeJsonHelper.GetVersionNoRuleList()
                    .ToDictionary(r => r.Id, r => r.RuleName);
                List<RecipeGridVM> list = recipes.Select(r =>
                {
                    string ruleName = r.VersionNoRuleId > 0 && ruleNameMap.TryGetValue(r.VersionNoRuleId, out string? rn)
                        ? rn : "—";
                    string currentVersionNo = RecipeJsonHelper.GetCurrentOrLatestVersion(r.Id)?.VersionNo ?? "";
                    return MapToGridVM(r, RecipeJsonHelper.GetVersionCount(r.Id), ruleName, currentVersionNo);
                }).ToList();
                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>依 Id 取得單筆配方資料（用於載入編輯表單）</summary>
        [HttpGet("GetById")]
        public ActionResult GetById(long id)
        {
            try
            {
                RecipeData? recipe = RecipeJsonHelper.GetRecipeById(id);
                if (recipe == null)
                    return JsonValidFail("查無配方資料");

                return JsonSuccess(MapToEditVM(recipe));
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>取得指定配方的版本歷程清單</summary>
        [HttpGet("GetVersionList")]
        public ActionResult GetVersionList(long recipeId)
        {
            try
            {
                List<RecipeVersionVM> list = RecipeJsonHelper.GetVersionList(recipeId)
                                                             .Select(MapToVersionVM)
                                                             .ToList();
                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 查詢 --

        #region -- 新增 / 修改 --

        /// <summary>儲存配方基本資料（Id=0 為新增，Id>0 為修改）</summary>
        [HttpPost("Save")]
        public ActionResult Save([FromBody] RecipeEditVM vm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vm.RecipeNo))
                    return JsonValidFail("配方編號為必填");
                if (string.IsNullOrWhiteSpace(vm.RecipeName))
                    return JsonValidFail("配方名稱為必填");
                if (string.IsNullOrWhiteSpace(vm.Engineer))
                    return JsonValidFail("負責工程師為必填");

                if (vm.Id == 0 && vm.VersionNoRuleId <= 0)
                    return JsonValidFail("新增配方必須選擇版號規則");

                RecipeData data = new();
                data.Id = vm.Id;
                data.RecipeNo = vm.RecipeNo.Trim();
                data.RecipeName = vm.RecipeName.Trim();
                data.CurrentVersionNo = vm.CurrentVersionNo?.Trim() ?? "";
                data.Engineer = vm.Engineer.Trim();
                data.RecipeContent = vm.RecipeContent?.Trim() ?? "";
                data.Creator = 1;
                data.Updater = 1;
                data.Status = 1; // 新建預設啟用
                data.VersionNoRuleId = vm.VersionNoRuleId;

                if (vm.Id == 0)
                    RecipeJsonHelper.CreateRecipe(data);
                else
                    RecipeJsonHelper.UpdateRecipe(data);

                return JsonSuccess(new { IsSuccess = "Y", ReturnCode = vm.Id == 0 ? "新增成功" : "修改成功" });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 新增 / 修改 --

        #region -- 刪除 --

        /// <summary>軟刪除單筆配方（Status 設為 9）</summary>
        [HttpDelete("Delete")]
        public ActionResult Delete(long id)
        {
            try
            {
                bool ok = RecipeJsonHelper.DeleteRecipe(id);
                if (!ok)
                    return JsonValidFail("查無資料或已刪除");

                return JsonSuccess(new { IsSuccess = "Y", ReturnCode = "刪除成功" });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>批次軟刪除配方（傳入逗號分隔的 ID 字串）</summary>
        [HttpDelete("BatchDelete")]
        public ActionResult BatchDelete([FromQuery] string ids)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ids))
                    return JsonValidFail("未選擇任何資料");

                List<long> idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => long.TryParse(s.Trim(), out long n) ? n : 0L)
                                       .Where(n => n > 0)
                                       .ToList();

                if (!idList.Any())
                    return JsonValidFail("無效的資料 ID");

                RecipeJsonHelper.BatchDeleteRecipe(idList);
                return JsonSuccess(new { IsSuccess = "Y", ReturnCode = $"已刪除 {idList.Count} 筆資料" });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 刪除 --

        #region -- 設定現行版本 --

        /// <summary>將指定版本設為現行版本</summary>
        [HttpPost("SetCurrentVersion")]
        public ActionResult SetCurrentVersion([FromBody] SetCurrentVersionRequest req)
        {
            try
            {
                bool ok = RecipeJsonHelper.SetCurrentVersion(req.VersionId);
                if (!ok)
                    return JsonValidFail("查無版本資料");

                return JsonSuccess(new { IsSuccess = "Y", ReturnCode = "已切換為現行版本" });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 設定現行版本 --

        #region -- 新增版本 --

        /// <summary>提交新版本（依對應規則自動產生版號，並同步更新配方主檔 RecipeContent）</summary>
        [HttpPost("CreateVersion")]
        public ActionResult CreateVersion([FromBody] CreateVersionRequest req)
        {
            try
            {
                if (req.RecipeId <= 0)
                    return JsonValidFail("配方ID無效");
                if (string.IsNullOrWhiteSpace(req.VersionLog))
                    return JsonValidFail("異動紀錄為必填");

                RecipeData? recipe = RecipeJsonHelper.GetRecipeById(req.RecipeId);
                if (recipe == null)
                    return JsonValidFail("查無配方資料");

                string bumpType = req.VersionBumpType?.Trim() ?? "PATCH";

                // 必須設定版號規則
                if (recipe.VersionNoRuleId <= 0)
                    return JsonValidFail("此配方尚未設定版號規則，無法提交版本");

                RecipeVersionNoRuleData? rule = RecipeJsonHelper.GetVersionNoRuleById(recipe.VersionNoRuleId);
                if (rule == null)
                    return JsonValidFail("版號規則不存在或已廢止，無法提交版本");

                // 從現行版本的 VersionSeed（JSON）讀取目前種子；不含 JSON 則視為首次使用規則
                RecipeVersionData? latest = RecipeJsonHelper.GetCurrentOrLatestVersion(req.RecipeId);
                bool hasValidSeed = !string.IsNullOrEmpty(latest?.VersionSeed)
                    && latest.VersionSeed.TrimStart().StartsWith("{");
                VersionSeedData currentSeed = hasValidSeed
                    ? VersionNoRuleHelper.DeserializeSeed(latest!.VersionSeed)
                    : VersionNoRuleHelper.GetInitialSeed(rule.RuleSetting);
                VersionSeedData newSeed = hasValidSeed
                    ? VersionNoRuleHelper.CalculateNewSeed(rule.RuleSetting, currentSeed, bumpType)
                    : currentSeed;

                string autoVersionNo = VersionNoRuleHelper.GenerateVersionNo(rule.RuleSetting, newSeed, bumpType, recipe.RecipeNo);
                string seedJson = VersionNoRuleHelper.SerializeSeed(newSeed);

                RecipeVersionData data = new();
                data.RecipeId = req.RecipeId;
                data.VersionNo = autoVersionNo;
                data.VersionLog = req.VersionLog.Trim();
                data.RecipeContent = req.RecipeContent ?? "{}";
                data.IsCurrent = req.IsCurrent;
                data.VersionSeed = seedJson;
                data.VersionBumpType = bumpType;
                data.Creator = 1;
                data.Updater = 1;
                data.Status = 1;

                RecipeJsonHelper.CreateVersion(data);

                // 同步更新 TB_Recipe.RecipeContent
                if (!string.IsNullOrEmpty(req.RecipeContent))
                {
                    recipe.RecipeContent = req.RecipeContent.Trim();
                    RecipeJsonHelper.UpdateRecipe(recipe);
                }

                return JsonSuccess(new { IsSuccess = "Y", ReturnCode = "版本提交成功", VersionNo = autoVersionNo });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 新增版本 --

        #region -- 輔助查詢 --

        /// <summary>取得未作廢的版號規則清單（用於新增配方時的下拉選單）</summary>
        [HttpGet("GetVersionNoRuleOptions")]
        public ActionResult GetVersionNoRuleOptions()
        {
            try
            {
                var options = RecipeJsonHelper.GetVersionNoRuleList()
                    .Select(r => new { r.Id, r.RuleName })
                    .ToList();
                return JsonSuccess(options);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>取得下一個版號預覽（依配方 ID 與升版類型）</summary>
        [HttpGet("GetNextVersionPreview")]
        public ActionResult GetNextVersionPreview(long recipeId, string bumpType = "PATCH")
        {
            try
            {
                RecipeData? recipe = RecipeJsonHelper.GetRecipeById(recipeId);
                if (recipe == null)
                    return JsonSuccess(new { Preview = "" });

                if (recipe.VersionNoRuleId <= 0)
                    return JsonSuccess(new { Preview = "（未設定版號規則）" });

                RecipeVersionNoRuleData? rule = RecipeJsonHelper.GetVersionNoRuleById(recipe.VersionNoRuleId);
                if (rule == null)
                    return JsonSuccess(new { Preview = "（規則不存在）" });

                RecipeVersionData? latest = RecipeJsonHelper.GetCurrentOrLatestVersion(recipeId);
                bool hasValidSeed = !string.IsNullOrEmpty(latest?.VersionSeed)
                    && latest.VersionSeed.TrimStart().StartsWith("{");
                VersionSeedData currentSeed = hasValidSeed
                    ? VersionNoRuleHelper.DeserializeSeed(latest!.VersionSeed)
                    : VersionNoRuleHelper.GetInitialSeed(rule.RuleSetting);
                VersionSeedData previewSeed = hasValidSeed
                    ? VersionNoRuleHelper.CalculateNewSeed(rule.RuleSetting, currentSeed, bumpType)
                    : currentSeed;

                string preview = VersionNoRuleHelper.GenerateVersionNo(rule.RuleSetting, previewSeed, bumpType, recipe.RecipeNo);
                return JsonSuccess(new { Preview = preview });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>依配方編號下載現行版本的配方內容文字檔</summary>
        [HttpGet("DownloadRecipeContent")]
        public ActionResult DownloadRecipeContent(string recipeNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeNo))
                    return JsonValidFail("配方編號為必填");

                RecipeData? recipe = RecipeJsonHelper.GetRecipeByRecipeNo(recipeNo.Trim());
                if (recipe == null)
                    return JsonValidFail("查無配方資料");

                RecipeVersionData? version = RecipeJsonHelper.GetCurrentOrLatestVersion(recipe.Id);
                if (version == null)
                    return JsonValidFail("此配方尚無任何版本");

                string content = version.RecipeContent;
                string versionNo = version.VersionNo;
                string fileName = $"{recipe.RecipeNo}_{versionNo}.txt";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/plain; charset=utf-8", fileName);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 輔助查詢 --

        /// <summary>統一錯誤處理，回傳包含錯誤訊息的 DispatcherReturnMsg。</summary>
        protected DispatcherReturnMsg HandleError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            DispatcherReturnMsg dispatcherReturnMsg = new();
            dispatcherReturnMsg.IsSuccess = "N";
            dispatcherReturnMsg.ReturnMsg = ex.Message;
            dispatcherReturnMsg.ReturnCode = "E500";
            dispatcherReturnMsg.AlertLevel = "error";
            return dispatcherReturnMsg;
        }
    }

    /// <summary>SetCurrentVersion API 的請求本體</summary>
    public class SetCurrentVersionRequest
    {
        /// <summary>要設為現行版本的版本 ID</summary>
        public long VersionId { get; set; }
    }

    /// <summary>CreateVersion API 的請求本體</summary>
    public class CreateVersionRequest
    {
        /// <summary>所屬配方 ID</summary>
        public long RecipeId { get; set; }
        /// <summary>版號（目前由後端自動產生，此欄位保留備用）</summary>
        public string VersionNo { get; set; } = "";
        /// <summary>異動紀錄（必填）</summary>
        public string VersionLog { get; set; } = "";
        /// <summary>配方內容 JSON</summary>
        public string RecipeContent { get; set; } = "{}";
        /// <summary>是否設為現行版本</summary>
        public bool IsCurrent { get; set; } = true;
        /// <summary>版號種子類型（MAJOR / MINOR / PATCH）</summary>
        public string VersionSeed { get; set; } = "PATCH";
        /// <summary>本次提交的升版類型（MAJOR / MINOR / PATCH）</summary>
        public string VersionBumpType { get; set; } = "PATCH";
    }
}
