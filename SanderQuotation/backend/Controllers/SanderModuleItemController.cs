using AutoMapper;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Helper.Excel;
using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace backend.Controllers
{
    /// <summary>
    /// Sander 採購型號主檔 API Controller
    /// </summary>
    [Route("api/SanderModuleItem")]
    public partial class SanderModuleItemController : BaseProjectController
    {
        /// <summary>
        /// 功能說明：建立採購型號主檔 Controller，設定 AutoMapper 與 Web 根目錄（匯入 Excel 用）。
        /// </summary>
        /// <param name="configuration">輸入參數：應用程式組態。</param>
        /// <param name="webHostEnvironment">輸入參數：取得 wwwroot 下範本檔路徑。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController；Mapper SanderModuleItemDM ↔ SanderModuleItemVM。
        /// 訊息內容及生成條件：建構子本身不產生 API 回應。
        /// </remarks>
        public SanderModuleItemController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment) : base(configuration)
        {
            _webHostEnvironment = webHostEnvironment;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<SanderModuleItemDM, SanderModuleItemVM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// 功能說明：分頁取得採購型號清單（從資料庫）。
        /// </summary>
        /// <param name="filter">輸入參數：分頁、排序、KeywordLike 等（SanderModuleItemSearchVM）。</param>
        /// <returns>輸出參數：JsonSuccess({ Data, Total, Page, PageSize })；例外時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetPageEntity、GetBlSanderModuleItem().GetPageList、_mapper.Map。
        /// 訊息內容及生成條件：成功 → 分頁清單 JSON；例外 → LogError 後 System_Error。
        /// </remarks>
        [HttpGet("GetPageList")]
        public ActionResult GetPageList([FromQuery] SanderModuleItemSearchVM filter)
        {
            try
            {
                PageEntity pageEntity = GetPageEntity<SanderModuleItemVM>(filter);

                SearchVO searchVO = new();
                searchVO.KeywordLike = filter.KeywordLike;

                PageResult<SanderModuleItemDM> pageResult = GetBlSanderModuleItem().GetPageList(pageEntity, searchVO);

                List<SanderModuleItemVM> list = [];
                foreach (SanderModuleItemDM dm in pageResult.Results)
                {
                    SanderModuleItemVM vm = _mapper.Map<SanderModuleItemVM>(dm);

                    list.Add(vm);
                }

                return JsonSuccess(new
                {
                    Data = list,
                    Total = pageResult.DataCount,
                    Page = pageResult.CurrentPage,
                    PageSize = pageResult.PageDataSize,
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }

    public partial class SanderModuleItemController
    {
        /// <summary>
        /// 功能說明：從 wwwroot/file/SanderModule_Item.xlsx 匯入內部料號至資料庫（開發/維護用，非公開 API）。
        /// </summary>
        /// <remarks>
        /// 參考功能名稱與用途：ExcelReaderHelper — 讀取 Excel；GetBlSanderModuleItem().DoCreate — 逐筆新增。
        /// 訊息內容及生成條件：例外時 LogError，無 HTTP 回應。
        /// </remarks>
        private void ImportSandermoduleItem()
        {
            try
            {
                string pathTmpl = Path.Combine(_webHostEnvironment.WebRootPath, "file", "SanderModule_Item.xlsx");
                ExcelReaderHelper reader = new();
                reader.SetWorkBook(pathTmpl);
                reader.SetSheet(reader.GetWorkBook().GetSheetAt(0));
                List<SanderModuleItemDM> dmList = [];
                int startRowIndex = 2;
                for (int i = startRowIndex; i <= reader.GetSheet().LastRowNum; i++)
                {
                    SanderModuleItemDM dm = new();
                    reader.SetRowCellIndex(i, 0);
                    dm.No = reader.GetStringValue()?.Trim() ?? string.Empty;
                    reader.NextCell();
                    dm.Description = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.Description2 = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.LongDesc = reader.GetStringValue()?.Trim() ?? string.Empty;
                    reader.NextCell();
                    dm.LongDesc2 = reader.GetStringValue()?.Trim();

                    dmList.Add(dm);
                }
                reader.GetWorkBook().Dispose();

                foreach (SanderModuleItemDM dm in dmList)
                {
                    GetBlSanderModuleItem().DoCreate(dm);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        /// <summary>
        /// 功能說明：從 SanderModuleItemVariant.xlsx 匯入料品 Variant 對照資料。
        /// </summary>
        /// <remarks>
        /// 參考功能名稱與用途：GetBlSanderModuleItemVariant().DoCreate。
        /// 訊息內容及生成條件：例外時 LogError。
        /// </remarks>
        private void ImportSanderModuleItemVariant()
        {
            try
            {
                string pathTmpl = Path.Combine(_webHostEnvironment.WebRootPath, "file", "SanderModuleItemVariant.xlsx");
                ExcelReaderHelper reader = new();
                reader.SetWorkBook(pathTmpl);
                reader.SetSheet(reader.GetWorkBook().GetSheetAt(0));
                List<SanderModuleItemVariantDM> dmList = [];
                int startRowIndex = 2;
                for (int i = startRowIndex; i <= reader.GetSheet().LastRowNum; i++)
                {
                    SanderModuleItemVariantDM dm = new();
                    reader.SetRowCellIndex(i, 0);
                    dm.ItemNo = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.Code = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.Description = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.Description2 = reader.GetStringValue()?.Trim();

                    dmList.Add(dm);
                }
                reader.GetWorkBook().Dispose();

                foreach (SanderModuleItemVariantDM dm in dmList)
                {
                    GetBlSanderModuleItemVariant().DoCreate(dm);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        /// <summary>
        /// 功能說明：從 Report_Item_Customer.xlsx 匯入客戶代碼與 Variant Code 對照。
        /// </summary>
        /// <remarks>
        /// 參考功能名稱與用途：GetBlReportItemCustomer().DoCreate。
        /// 訊息內容及生成條件：例外時 LogError。
        /// </remarks>
        private void ImportReportItemCustomer()
        {
            try
            {
                string pathTmpl = Path.Combine(_webHostEnvironment.WebRootPath, "file", "Report_Item_Customer.xlsx");
                ExcelReaderHelper reader = new();
                reader.SetWorkBook(pathTmpl);
                reader.SetSheet(reader.GetWorkBook().GetSheetAt(0));
                List<ReportItemCustomerDM> dmList = [];
                int startRowIndex = 1;
                for (int i = startRowIndex; i <= reader.GetSheet().LastRowNum; i++)
                {
                    ReportItemCustomerDM dm = new();
                    reader.SetRowCellIndex(i, 0);
                    dm.VariantCode = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.CustomerCode = reader.GetStringValue()?.Trim();

                    dmList.Add(dm);
                }
                reader.GetWorkBook().Dispose();

                foreach (ReportItemCustomerDM dm in dmList)
                {
                    GetBlReportItemCustomer().DoCreate(dm);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        /// <summary>
        /// 功能說明：從 SanderModule_PurchaseLine.xlsx 匯入內部採購紀錄（價格分群資料來源）。
        /// </summary>
        /// <remarks>
        /// 參考功能名稱與用途：GetBlSanderModulePurchaseLine().DoCreate。
        /// 訊息內容及生成條件：例外時 LogError。
        /// </remarks>
        private void ImportSanderModulePurchaseLine()
        {
            try
            {
                string pathTmpl = Path.Combine(_webHostEnvironment.WebRootPath, "file", "SanderModule_PurchaseLine.xlsx");
                ExcelReaderHelper reader = new();
                reader.SetWorkBook(pathTmpl);
                reader.SetSheet(reader.GetWorkBook().GetSheetAt(0));
                List<SanderModulePurchaseLineDM> dmList = [];
                int startRowIndex = 1;
                for (int i = startRowIndex; i <= reader.GetSheet().LastRowNum; i++)
                {
                    SanderModulePurchaseLineDM dm = new();
                    reader.SetRowCellIndex(i, 0);
                    double? documentDateNum = reader.GetDoubleValue();
                    dm.DocumentDate = documentDateNum.HasValue
                        ? DateTime.FromOADate(documentDateNum.Value)
                        : null;
                    reader.NextCell();
                    dm.No = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.BuyFromVendorNo = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.BuyFromVendorName = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.UnitCost = (decimal?)reader.GetDoubleValue();
                    reader.NextCell();
                    dm.UnitCostLcy = (decimal?)reader.GetDoubleValue();
                    reader.NextCell();
                    dm.Quantity = (int?)reader.GetDoubleValue();
                    reader.NextCell();
                    dm.CurrencyCode = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.Description2 = reader.GetStringValue()?.Trim();

                    dmList.Add(dm);
                }
                reader.GetWorkBook().Dispose();

                foreach (SanderModulePurchaseLineDM dm in dmList)
                {
                    GetBlSanderModulePurchaseLine().DoCreate(dm);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
    }
}

