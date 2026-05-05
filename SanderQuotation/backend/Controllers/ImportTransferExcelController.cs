using backend.Models;
using CommonClass.Models;
using Core.Utility.Web.Base;
using Microsoft.AspNetCore.Mvc;
using ViewModel.ImportTransferExcel;

namespace backend.Controllers
{
    /// <summary>轉入檔案上傳 API Controller</summary>
    [Route("api/ImportTransferExcel")]
    public class ImportTransferExcelController : ApiBaseController
    {
        #region -- 私有輔助 --

        /// <summary>將 ImportTransferExcelData 對應至 Grid ViewModel。</summary>
        private static ImportTransferExcelGridVM MapToGridVM(ImportTransferExcelData d)
        {
            ImportTransferExcelGridVM vm = new();
            vm.Id = d.Id;
            vm.BomFileName = d.BomFileName;
            vm.ImportRuleName = d.ImportRuleName;
            vm.CustomerName = d.CustomerName;
            vm.ProductNo = d.ProductNo;
            vm.CustomerType = d.CustomerType;
            vm.PurchaseQty = d.PurchaseQty;
            vm.StatusCode = d.StatusCode;
            vm.Status = ImportTransferExcelJsonHelper.StatusText(d.StatusCode);
            vm.CreateTime = d.CreateTime.ToString("yyyy/MM/dd");
            vm.UpdateTime = d.UpdateTime.ToString("yyyy/MM/dd");
            return vm;
        }

        /// <summary>統一錯誤處理，回傳包含錯誤訊息的 DispatcherReturnMsg。</summary>
        private DispatcherReturnMsg HandleError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ImportTransferExcel Error: {ex}");
            DispatcherReturnMsg msg = new();
            msg.IsSuccess = "N";
            msg.ReturnMsg = ex.Message;
            msg.ReturnCode = "E500";
            msg.AlertLevel = "error";
            return msg;
        }

        #endregion

        #region -- 查詢 --

        /// <summary>取得轉入檔案清單（支援關鍵字搜尋）。</summary>
        [HttpGet("GetList")]
        public ActionResult GetList(string? keyword = null)
        {
            try
            {
                List<ImportTransferExcelData> data = ImportTransferExcelJsonHelper.GetList(keyword);
                List<ImportTransferExcelGridVM> list = data.Select(MapToGridVM).ToList();
                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>取得上傳設定用的下拉選項資料（匯入設定規則、客戶名稱）。</summary>
        [HttpGet("GetOptionData")]
        public ActionResult GetOptionData()
        {
            try
            {
                List<string> importRules = new()
                {
                    "標準BOM規則",
                    "客製規則A",
                    "客製規則B",
                    "精簡BOM規則"
                };

                List<string> customerNames = new()
                {
                    "鴻海精密",
                    "廣達電腦",
                    "仁寶電腦",
                    "緯創資通",
                    "英業達股份",
                    "和碩聯合",
                    "緯穎科技"
                };

                var result = new
                {
                    ImportRules = importRules,
                    CustomerNames = customerNames
                };

                return JsonSuccess(result);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion

        #region -- 編輯 --

        /// <summary>儲存編輯（僅允許更新客戶名稱與產品料號）。</summary>
        [HttpPost("SaveEdit")]
        public ActionResult SaveEdit([FromBody] SaveEditRequest req)
        {
            try
            {
                if (req == null || req.Id <= 0)
                    return JsonValidFail("資料有誤，請重新操作。");

                if (string.IsNullOrWhiteSpace(req.CustomerName))
                    return JsonValidFail("客戶名稱不可空白。");

                ImportTransferExcelJsonHelper.SaveEdit(req.Id, req.CustomerName.Trim(), req.ProductNo?.Trim() ?? "");
                return JsonSuccess("儲存成功。");
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion

        #region -- 刪除 --

        /// <summary>依 Id 刪除一筆轉入檔案紀錄。</summary>
        [HttpPost("Delete")]
        public ActionResult Delete([FromBody] DeleteRequest req)
        {
            try
            {
                if (req == null || req.Id <= 0)
                    return JsonValidFail("資料有誤。");

                bool deleted = ImportTransferExcelJsonHelper.Delete(req.Id);
                if (!deleted) return JsonValidFail("找不到指定的紀錄。");

                return JsonSuccess("刪除成功。");
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion

        #region -- 請求模型 --

        /// <summary>編輯請求模型。</summary>
        public class SaveEditRequest
        {
            /// <summary>主鍵</summary>
            public long Id { get; set; }

            /// <summary>客戶名稱</summary>
            public string CustomerName { get; set; } = "";

            /// <summary>產品料號</summary>
            public string ProductNo { get; set; } = "";
        }

        /// <summary>刪除請求模型。</summary>
        public class DeleteRequest
        {
            /// <summary>主鍵</summary>
            public long Id { get; set; }
        }

        #endregion
    }
}
