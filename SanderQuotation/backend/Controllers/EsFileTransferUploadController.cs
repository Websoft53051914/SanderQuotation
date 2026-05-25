using AutoMapper;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    /// <summary>
    /// 轉入檔案上傳 API Controller
    /// </summary>
    [Route("api/EsFileTransferUpload")]
    public partial class EsFileTransferUploadController : BaseProjectController
    {
        private readonly IMapper _mapper;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        public EsFileTransferUploadController(IConfiguration configuration) : base(configuration)
        {
            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<EsFileTransferUploadDM, EsFileTransferUploadVM>()
                    .ForMember(d => d.No, o => o.Ignore())
                    .ForMember(d => d.FileName, o => o.MapFrom(s => s.FileName ?? string.Empty))
                    .ForMember(d => d.QuotationQty, o => o.MapFrom(s => s.QuotationQty ?? 0))
                    .ForMember(d => d.ProcessStatus, o => o.MapFrom(s => s.ProcessStatus ?? (int)EsFileTransferUploadProcessStatusEnum.Pending))
                    .ForMember(d => d.ProcessStatusText, o => o.Ignore())
                    .ForMember(d => d.CreatedAtText, o => o.Ignore())
                    .ForMember(d => d.UpdatedAtText, o => o.Ignore());
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 將處理狀態代碼轉換為對應的中文描述文字
        /// </summary>
        /// <param name="code">ProcessStatus 代碼</param>
        /// <returns>對應的中文描述，找不到時回傳代碼字串</returns>
        private static string GetProcessStatusText(int? code)
        {
            return code switch
            {
                (int)EsFileTransferUploadProcessStatusEnum.Pending => EsFileTransferUploadProcessStatusEnum.Pending.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.Transferred => EsFileTransferUploadProcessStatusEnum.Transferred.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.PendingPartSearch => EsFileTransferUploadProcessStatusEnum.PendingPartSearch.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.PartSearchDone => EsFileTransferUploadProcessStatusEnum.PartSearchDone.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.PricingDone => EsFileTransferUploadProcessStatusEnum.PricingDone.GetDescription(),
                _ => code?.ToString() ?? string.Empty,
            };
        }

        private EsFileTransferUploadBL? _blEsFileTransferUpload = null;
        /// <summary>
        /// EsFileTransferUploadBL
        /// </summary>
        /// <returns></returns>
        protected EsFileTransferUploadBL GetBlEsFileTransferUpload()
        {
            _blEsFileTransferUpload ??= GetBLInstance<EsFileTransferUploadBL>();

            return _blEsFileTransferUpload;
        }

        private ESDbTransferMappingBL? _blESDbTransferMapping = null;
        /// <summary>
        /// ESDbTransferMappingBL
        /// </summary>
        /// <returns></returns>
        protected ESDbTransferMappingBL GetBlESDbTransferMapping()
        {
            _blESDbTransferMapping ??= GetBLInstance<ESDbTransferMappingBL>();

            return _blESDbTransferMapping;
        }

        private ReportItemCustomerBL? _blReportItemCustomer = null;
        /// <summary>
        /// ReportItemCustomerBL
        /// </summary>
        /// <returns></returns>
        protected ReportItemCustomerBL GetBlReportItemCustomer()
        {
            _blReportItemCustomer ??= GetBLInstance<ReportItemCustomerBL>();

            return _blReportItemCustomer;
        }

        private TableExcelBL? _blTableExcel = null;
        /// <summary>
        /// TableExcelBL
        /// </summary>
        /// <returns></returns>
        protected TableExcelBL GetBlTableExcel()
        {
            _blTableExcel ??= GetBLInstance<TableExcelBL>();

            return _blTableExcel;
        }
    }
    public partial class EsFileTransferUploadController
    {
        #region -- 查詢 --

        /// <summary>
        /// 分頁取得轉入檔案清單
        /// </summary>
        [CustomAuthorization(FuncID.ESDbTransferMapping_View)]
        [HttpGet("GetPageList")]
        public ActionResult GetPageList([FromQuery] EsFileTransferUploadSearchVM filter)
        {
            try
            {
                PageEntity pageEntity = GetPageEntity<EsFileTransferUploadVM>(filter);

                SearchVO searchVO = new();
                searchVO.KeywordLike = filter.KeywordLike;

                PageResult<EsFileTransferUploadDM> pageResult = GetBlEsFileTransferUpload().GetPageList(pageEntity, searchVO);
                int baseNo = (filter.Page - 1) * filter.PageSize;

                List<EsFileTransferUploadVM> list = new();
                int no = 0;
                foreach (EsFileTransferUploadDM d in pageResult.Results)
                {
                    EsFileTransferUploadVM vm = _mapper.Map<EsFileTransferUploadVM>(d);
                    vm.No = baseNo + no + 1;
                    vm.ProcessStatusText = GetProcessStatusText(d.ProcessStatus);
                    vm.CreatedAtText = d.CreatedAt?.ToString("yyyy/MM/dd") ?? string.Empty;
                    vm.UpdatedAtText = d.UpdatedAt?.ToString("yyyy/MM/dd") ?? string.Empty;
                    list.Add(vm);
                    no++;
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

        /// <summary>
        /// 取得上傳設定用的下拉選項（匯入規則、客戶代碼）
        /// </summary>
        [CustomAuthorization(FuncID.ESDbTransferMapping_View)]
        [HttpGet("GetOptionData")]
        public ActionResult GetOptionData()
        {
            try
            {
                List<SelectItemVO> selectListTransferMapping = new();
                SearchVO searchVOEftm = new();
                foreach (EsFileTransferMappingDM m in GetBlTableExcel().GetListWithBomFlag(searchVOEftm))
                {
                    SelectItemVO item = new(m.TransferMappingCode, m.Id.ToString());
                    item.IsBomFileRule = m.IsBomFileRule;
                    selectListTransferMapping.Add(item);
                }

                List<SelectItemVO> selectListCustomer = GetBlReportItemCustomer()
                    .GetListEnabled(new SearchVO())
                    .GroupBy(x => x.CustomerCode)
                    .Select(x => new SelectItemVO(x.First().CustomerName ?? string.Empty, x.Key ?? string.Empty))
                    .ToList();

                return JsonSuccess(new { TransferMappings = selectListTransferMapping, Customers = selectListCustomer });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion

        #region -- 編輯 --

        /// <summary>
        /// 儲存編輯
        /// </summary>
        [CustomAuthorization(FuncID.EsFileTransferUpload_Edit)]
        [HttpPost("SaveEdit")]
        public ActionResult SaveEdit([FromBody] EsFileTransferUploadVM req)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(req);
                ArgumentNullException.ThrowIfNull(req.Id);

                EsFileTransferUploadDM? dm = GetBlEsFileTransferUpload().GetOneInfo(req.Id.Value);
                if (dm == null) return JsonValidFail("資料不存在");

                dm.CustomerCode = req.CustomerCode?.Trim();
                GetBlEsFileTransferUpload().DoUpdateEdit(dm);

                return JsonOK("儲存成功。");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion

        #region -- 刪除 --

        /// <summary>
        /// 依 Id 刪除一筆轉入檔案紀錄（邏輯刪除）及所有關聯資料
        /// </summary>
        [CustomAuthorization(FuncID.EsFileTransferUpload_Delete)]
        [HttpPost("Delete")]
        public ActionResult Delete([FromBody] List<Guid> idList)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(idList);
                if (idList.Count == 0)
                    return JsonValidFail("請至少選擇一筆資料進行刪除。");

                GetBlEsFileTransferUpload().DoDelete(idList);

                return JsonOK("刪除成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion

        #region -- 上傳 --

        /// <summary>
        /// 儲存上傳設定，更新 FilePond 建立的暫存紀錄為正式資料
        /// </summary>
        [CustomAuthorization(FuncID.EsFileTransferUpload_Create)]
        [HttpPost("SaveUpload")]
        public ActionResult SaveUpload([FromBody] List<EsFileTransferUploadVM> list)
        {
            try
            {
                if (list == null || list.Count == 0)
                    return JsonValidFail("請至少上傳一個檔案。");

                foreach (EsFileTransferUploadVM vm in list)
                {
                    if (!vm.UploadId.HasValue)
                        return JsonValidFail("檔案不存在");
                    if (!vm.EsFileTransferMappingId.HasValue)
                        return JsonValidFail("匯入設定規則不可空白");

                    EsFileTransferUploadDM? dm = GetBlEsFileTransferUpload().GetOneInfoByUploadId(vm.UploadId.Value);
                    if (dm == null || dm.Status == (int)Enums.StatusEnum.Cancel)
                        return JsonValidFail($"檔案不存在(ID：{vm.UploadId})");
                    EsFileTransferMappingDM? dmEftm = GetBlTableExcel().GetOneWithBomFlag(vm.EsFileTransferMappingId.Value);
                    if (dmEftm == null)
                        return JsonValidFail($"匯入設定規則不存在（ID：{vm.EsFileTransferMappingId}）");

                    if (dmEftm.IsBomFileRule)
                    {
                        // 如果是BOM檔規則，則報價數量必須大於0
                        if (vm.QuotationQty <= 0)
                        {
                            return JsonValidFail("報價數量必須大於 0");
                        }
                        if (string.IsNullOrWhiteSpace(vm.CustomerCode) && string.IsNullOrWhiteSpace(vm.ManualCustomerName))
                            return JsonValidFail("客戶代碼或客戶名稱不可空白");
                    }

                    dm.EsFileTransferMappingId = dmEftm.Id;
                    dm.CustomerCode = vm.CustomerCode;
                    dm.ManualCustomerName = vm.ManualCustomerName?.Trim();
                    dm.QuotationQty = vm.QuotationQty;

                    GetBlEsFileTransferUpload().DoUpdateUpload(dm);
                }

                return JsonOK("上傳成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion

    }
}
