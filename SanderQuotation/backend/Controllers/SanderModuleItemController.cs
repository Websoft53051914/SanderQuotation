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
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
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
        /// 分頁取得採購型號清單（從資料庫）
        /// </summary>
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
        /// 匯入內部料號資料
        /// </summary>
        /// <returns></returns>
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
        /// 匯入各料品 Variant 資料
        /// </summary>
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
        /// 匯入客戶代碼與 Variant Code 對照表資料
        /// </summary>
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
        /// 匯入內部採購紀錄資料
        /// </summary>
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

