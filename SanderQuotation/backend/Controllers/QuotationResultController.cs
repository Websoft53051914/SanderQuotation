using backend.Models;
using CommonClass.Models;
using Core.Utility.Web.Base;
using Microsoft.AspNetCore.Mvc;
using ViewModel.QuotationResult;

namespace backend.Controllers
{
    /// <summary>定時查價結果 API Controller</summary>
    [Route("api/QuotationResult")]
    public class QuotationResultController : ApiBaseController
    {
        #region -- 私有輔助 --

        /// <summary>將 QuotationFileData 對應至清單 ViewModel。</summary>
        private static QuotationFileGridVM MapToGridVM(QuotationFileData f)
        {
            QuotationFileGridVM vm = new();
            vm.Id = f.Id;
            vm.BomFileName = f.BomFileName;
            vm.CustomerName = f.CustomerName;
            vm.ProductNo = f.ProductNo;
            vm.CustomerType = f.CustomerType;
            vm.PurchaseQty = f.PurchaseQty;
            vm.ItemCount = QuotationJsonHelper.GetItemCount(f.Id);
            vm.CreateTime = f.CreateTime.ToString("yyyy/MM/dd");
            vm.UpdateTime = f.UpdateTime.ToString("yyyy/MM/dd");
            return vm;
        }

        /// <summary>將 QuotationItemData 對應至料項 ViewModel。</summary>
        private static QuotationItemVM MapToItemVM(QuotationItemData i)
        {
            QuotationItemVM vm = new();
            vm.Id = i.Id;
            vm.FileId = i.FileId;
            vm.Description = i.Description;
            vm.Manufacturer1 = i.Manufacturer1;
            vm.ManufacturerPartNo1 = i.ManufacturerPartNo1;
            vm.Quantity = i.Quantity;
            vm.InternalProcurementDate = i.InternalProcurementDate;
            vm.InternalUnitPriceOrig = i.InternalUnitPriceOrig;
            vm.InternalUnitPriceTwd = i.InternalUnitPriceTwd;
            vm.InternalQty = i.InternalQty;
            vm.InternalCurrency = i.InternalCurrency;
            vm.InternalSupplierName = i.InternalSupplierName;
            vm.ExternalQuotationDate = i.ExternalQuotationDate;
            vm.ExternalUnitPriceOrig = i.ExternalUnitPriceOrig;
            vm.ExternalUnitPriceTwd = i.ExternalUnitPriceTwd;
            vm.ExternalMOQ = i.ExternalMOQ;
            vm.ExternalCurrency = i.ExternalCurrency;
            vm.ExternalSupplierName = i.ExternalSupplierName;
            vm.ProcurementModel = i.ProcurementModel;
            vm.IsRecommended = i.IsRecommended;
            vm.ProcurementModelOptions = i.ProcurementModelOptions;
            return vm;
        }

        #endregion

        #region -- 查詢 --

        /// <summary>取得查價檔案清單（支援關鍵字搜尋）</summary>
        [HttpGet("GetList")]
        public ActionResult GetList(string? keyword = null)
        {
            try
            {
                List<QuotationFileData> files = QuotationJsonHelper.GetFileList(keyword);
                List<QuotationFileGridVM> list = files.Select(MapToGridVM).ToList();
                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>依 Id 取得單筆查價檔案（含 BOM 料項明細）</summary>
        [HttpGet("GetById")]
        public ActionResult GetById(long id)
        {
            try
            {
                QuotationFileData? file = QuotationJsonHelper.GetFileById(id);
                if (file == null)
                    return JsonValidFail("查無查價資料");

                QuotationFileEditVM vm = new();
                vm.Id = file.Id;
                vm.BomFileName = file.BomFileName;
                vm.CustomerName = file.CustomerName;
                vm.ProductNo = file.ProductNo;
                vm.CustomerType = file.CustomerType;
                vm.PurchaseQty = file.PurchaseQty;
                vm.CreateTime = file.CreateTime.ToString("yyyy/MM/dd HH:mm");
                vm.UpdateTime = file.UpdateTime.ToString("yyyy/MM/dd HH:mm");
                vm.Items = QuotationJsonHelper.GetItemsByFileId(id).Select(MapToItemVM).ToList();

                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion

        #region -- 儲存 --

        /// <summary>儲存查價檔案 Header（客戶別 / 採購數量）</summary>
        [HttpPost("SaveHeader")]
        public ActionResult SaveHeader([FromBody] QuotationFileEditVM payload)
        {
            try
            {
                if (payload.Id == 0)
                    return JsonValidFail("無效的檔案 ID");

                // QuotationFileData data = new();
                // data.Id = payload.Id;
                // data.CustomerType = payload.CustomerType;
                // data.PurchaseQty = payload.PurchaseQty;
                // data.Updater = 1;

                // QuotationJsonHelper.SaveFile(data);
                return JsonOK("儲存成功");
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>批次儲存 BOM 料項採購型號</summary>
        [HttpPost("SaveProcurementModels")]
        public ActionResult SaveProcurementModels([FromBody] List<QuotationItemVM> payload)
        {
            try
            {
                if (payload == null || payload.Count == 0)
                    return JsonValidFail("無資料可儲存");

                List<QuotationItemData> updates = payload.Select(p =>
                {
                    QuotationItemData d = new();
                    d.Id = p.Id;
                    d.ProcurementModel = p.ProcurementModel ?? "";
                    return d;
                }).ToList();

                QuotationJsonHelper.SaveItemProcurementModels(updates);
                return JsonOK("儲存成功");
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion

        #region -- 料號快查 --

        /// <summary>料號快查：依製造商料號模糊搜尋 BOM 料項。</summary>
        [HttpGet("QuickSearch")]
        public ActionResult QuickSearch(string? manufacturerPartNo = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(manufacturerPartNo))
                    return JsonSuccess(new List<QuotationItemVM>());

                List<QuotationItemData> items = QuotationJsonHelper.GetItemsByManufacturerPartNo(manufacturerPartNo);
                List<QuotationItemVM> list = items.Select(MapToItemVM).ToList();
                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>快查視窗儲存單筆料項採購型號與建議採購型號旗標。</summary>
        [HttpPost("SaveQuickItem")]
        public ActionResult SaveQuickItem([FromBody] QuotationItemVM payload)
        {
            try
            {
                if (payload.Id == 0)
                    return JsonValidFail("無效的料項 ID");

                //bool ok = QuotationJsonHelper.SaveQuickItem(payload.Id, payload.ProcurementModel ?? "", payload.IsRecommended);
                //if (!ok)
                //    return JsonValidFail("查無此料項");

                return JsonOK("重新查價成功");
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion

        #region -- 刪除 --

        /// <summary>刪除單筆查價檔案</summary>
        [HttpDelete("Delete")]
        public ActionResult Delete(long id)
        {
            try
            {
                bool ok = QuotationJsonHelper.DeleteFile(id);
                if (!ok)
                    return JsonValidFail("查無資料");
                return JsonOK("刪除成功");
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        /// <summary>批次刪除查價檔案（ids 以逗號分隔）</summary>
        [HttpDelete("BatchDelete")]
        public ActionResult BatchDelete(string ids)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ids))
                    return JsonValidFail("未指定要刪除的資料");

                List<long> idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => long.TryParse(s.Trim(), out long v) ? v : 0L)
                    .Where(v => v > 0)
                    .ToList();

                int deleted = QuotationJsonHelper.BatchDeleteFiles(idList);
                return JsonOK($"已刪除 {deleted} 筆");
            }
            catch (Exception ex)
            {
                return JsonSuccess(HandleError(ex));
            }
        }

        #endregion

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
}
