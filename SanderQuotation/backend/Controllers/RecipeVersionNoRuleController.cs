using CommonClass.Models;
using Core.Utility.Web.Base;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using ViewModel.RecipeMgmt;

namespace backend.Controllers
{
    /// <summary>
    /// 版號規則設定 API Controller（對應 TB_RecipeVersionNoRule）
    /// </summary>
    [Route("api/RecipeVersionNoRule")]
    public class RecipeVersionNoRuleController : ApiBaseController
    {
        #region -- 狀態對照 --

        private static readonly Dictionary<int, (string Name, string BadgeCss)> StatusMap = new()
        {
            { 0, ("草稿", "badge bg-warning-subtle text-warning") },
            { 1, ("啟用", "badge bg-success-subtle text-success") },
            { 2, ("停用", "badge bg-secondary-subtle text-secondary") },
            { 9, ("已廢止", "badge bg-dark-subtle text-dark") },
        };

        #endregion -- 狀態對照 --

        #region -- 查詢 --

        /// <summary>取得版號規則清單（支援關鍵字搜尋）</summary>
        [HttpGet("GetList")]
        public ActionResult GetList(string? keyword = null)
        {
            try
            {
                List<RecipeVersionNoRuleVM> list = RecipeJsonHelper.GetVersionNoRuleList(keyword)
                    .Select(MapToVM)
                    .ToList();

                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>依 Id 取得單筆版號規則（用於載入編輯表單）</summary>
        [HttpGet("GetById")]
        public ActionResult GetById(long id)
        {
            try
            {
                RecipeVersionNoRuleData? rule = RecipeJsonHelper.GetVersionNoRuleById(id);
                if (rule == null)
                    return JsonValidFail("查無版號規則資料");

                RecipeVersionNoRuleEditVM editVm = new();
                editVm.Id = rule.Id;
                editVm.RuleName = rule.RuleName;
                editVm.RuleSetting = rule.RuleSetting;
                editVm.StatusCode = rule.Status;
                return JsonSuccess(editVm);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 查詢 --

        #region -- 新增 / 修改 --

        /// <summary>儲存版號規則（Id=0 為新增，Id>0 為修改）</summary>
        [HttpPost("Save")]
        public ActionResult Save([FromBody] RecipeVersionNoRuleEditVM vm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vm.RuleName))
                    return JsonValidFail("規則名稱為必填");

                if (string.IsNullOrWhiteSpace(vm.RuleSetting))
                    return JsonValidFail("區段設定不能為空");

                RecipeVersionNoRuleData data = new();
                data.Id = vm.Id;
                data.RuleName = vm.RuleName.Trim();
                data.RuleSetting = vm.RuleSetting;
                data.Status = vm.StatusCode;
                data.Creator = 1;
                data.Updater = 1;

                if (vm.Id == 0)
                    RecipeJsonHelper.CreateVersionNoRule(data);
                else
                    RecipeJsonHelper.UpdateVersionNoRule(data);

                return JsonSuccess(new
                {
                    IsSuccess = "Y",
                    ReturnCode = vm.Id == 0 ? "新增成功" : "修改成功",
                });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 新增 / 修改 --

        #region -- 刪除 --

        /// <summary>軟刪除版號規則（Status 設為 9）</summary>
        [HttpDelete("Delete")]
        public ActionResult Delete(long id)
        {
            try
            {
                bool ok = RecipeJsonHelper.DeleteVersionNoRule(id);
                if (!ok)
                    return JsonValidFail("查無資料或已刪除");

                return JsonSuccess(new { IsSuccess = "Y", ReturnCode = "刪除成功" });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 刪除 --

        #region -- 工具端點 --

        /// <summary>
        /// 依傳入的 RuleSetting JSON 即時產生版號預覽。
        /// <br/>前端在設計區段時可呼叫此端點取得即時預覽結果。
        /// </summary>
        [HttpPost("GeneratePreview")]
        public ActionResult GeneratePreview([FromBody] GeneratePreviewRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.RuleSetting))
                    return JsonSuccess(new { Preview = "" });

                string preview = VersionNoRuleHelper.GeneratePreview(
                    req.RuleSetting, req.RecipeNo);

                return JsonSuccess(new { Preview = preview });
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion -- 工具端點 --

        #region -- 私有輔助 --

        /// <summary>將 RecipeVersionNoRuleData 對應至清單 ViewModel。</summary>
        private static RecipeVersionNoRuleVM MapToVM(RecipeVersionNoRuleData r)
        {
            (string Name, string BadgeCss) status = StatusMap.TryGetValue(r.Status, out (string Name, string BadgeCss) s)
                ? s
                : ("未知", "badge bg-secondary");

            // 產生版號預覽；解析失敗時顯示空值
            string preview = "";
            try
            {
                preview = VersionNoRuleHelper.GeneratePreview(r.RuleSetting);
            }
            catch { /* RuleSetting 格式錯誤時不顯示預覽 */ }

            RecipeVersionNoRuleVM vm = new();

            vm.Id = r.Id;
            vm.RuleName = r.RuleName;
            vm.StatusCode = r.Status;
            vm.PreviewVersion = preview;
            vm.CreateTime = r.CreateTime.ToString("yyyy/MM/dd");
            vm.UpdateTime = r.UpdateTime.ToString("yyyy/MM/dd HH:mm");
            return vm;
        }

        /// <summary>統一錯誤處理，回傳包含錯誤訊息的 DispatcherReturnMsg。</summary>
        private DispatcherReturnMsg HandleError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RecipeVersionNoRule Error] {ex}");
            DispatcherReturnMsg msg = new();
            msg.IsSuccess = "N";
            msg.ReturnMsg = ex.Message;
            msg.ReturnCode = "E500";
            msg.AlertLevel = "error";
            return msg;
        }

        #endregion -- 私有輔助 --
    }

    /// <summary>GeneratePreview API 的請求本體</summary>
    public class GeneratePreviewRequest
    {
        /// <summary>RuleSetting JSON 字串</summary>
        public string RuleSetting { get; set; } = "";

        /// <summary>示範用配方編號（RECIPE_NO 型別區段使用，可選）</summary>
        public string? RecipeNo { get; set; }
    }
}
