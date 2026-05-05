using backend.Models;
using CommonClass.Models;
using Core.Utility.Web.Base;
using Microsoft.AspNetCore.Mvc;
using ViewModel.SanderModule;

namespace backend.Controllers
{
    /// <summary>Sander 採購型號主檔 API Controller</summary>
    [Route("api/SanderModuleItem")]
    public class SanderModuleItemController : ApiBaseController
    {
        /// <summary>將 SanderModuleItemData 對應至 ViewModel。</summary>
        private static SanderModuleItemVM MapToVM(SanderModuleItemData d)
        {
            SanderModuleItemVM vm = new();
            vm.Id = d.Id;
            vm.No = d.No;
            vm.Description = d.Description;
            vm.Description2 = d.Description2;
            vm.LongDesc = d.LongDesc;
            vm.LongDesc2 = d.LongDesc2;
            return vm;
        }

        /// <summary>取得採購型號清單（支援關鍵字搜尋）</summary>
        [HttpGet("GetList")]
        public ActionResult GetList(string? keyword = null)
        {
            try
            {
                List<SanderModuleItemData> items = SanderModuleJsonHelper.GetList(keyword);
                List<SanderModuleItemVM> list = items.Select(MapToVM).ToList();
                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>統一錯誤處理，回傳包含錯誤訊息的 DispatcherReturnMsg。</summary>
        private DispatcherReturnMsg HandleError(Exception ex)
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
}
