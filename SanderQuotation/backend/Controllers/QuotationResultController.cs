using AutoMapper;
using backend.Common;
using backend.Common.Attribute;
using backend.Models;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Const.ApiModels.QueryPrice;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ViewModel;
using ViewModel.QuotationResult;
using static Const.Enums;

namespace backend.Controllers
{
    /// <summary>
    /// 定時查價結果 API Controller
    /// </summary>
    [Route("api/QuotationResult")]
    public partial class QuotationResultController : BaseProjectController
    {
        private readonly IMapper _mapper;
        private readonly QuotationHandler _quotationHandler;
        private readonly ExternalQueryExecuteHandler _externalQueryExecuteHandler;
        private readonly PathProvider _pathProvider;

        /// <summary>
        /// 功能說明：建立定時查價結果 Controller，注入查價處理器與外部 API 執行器，並設定 AutoMapper。
        /// </summary>
        /// <param name="configuration">輸入參數：應用程式組態。</param>
        /// <param name="quotationHandler">輸入參數：內部/外部查價與查料流程。</param>
        /// <param name="externalQueryExecuteHandler">輸入參數：Mouser/DigiKey 現貨查價。</param>
        /// <param name="pathProvider">輸入參數：檔案路徑中介站（匯出時讀取原始上傳檔）。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController；Mapper 對應 BomFileContent、QueryActionResult 至 DM/VM。
        /// 訊息內容及生成條件：建構子本身不產生 API 回應。
        /// </remarks>
        public QuotationResultController(
            IConfiguration configuration,
            QuotationHandler quotationHandler,
            ExternalQueryExecuteHandler externalQueryExecuteHandler,
            PathProvider pathProvider) : base(configuration)
        {
            _quotationHandler = quotationHandler;
            _externalQueryExecuteHandler = externalQueryExecuteHandler;
            _pathProvider = pathProvider;
            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<BomFileContentDM, QuotationItemVM>()
                    .ForMember(dest => dest.InternalPurchaseOrderDate,
                        opt => opt.MapFrom(src => src.InternalPurchaseOrderDate.HasValue
                            ? src.InternalPurchaseOrderDate.Value.ToString("yyyy/MM/dd")
                            : null))
                    .ForMember(dest => dest.ExternalQuotationDate,
                        opt => opt.MapFrom(src => src.ExternalQuotationDate.HasValue
                            ? src.ExternalQuotationDate.Value.ToString("yyyy/MM/dd")
                            : null));
                c.CreateMap<QueryActionResultRspVO.QueryResultRspVO, TBBomFileQuotationOtherDM>()
                    .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.ExternalCurrency));
                c.CreateMap<QueryActionResultRspVO.DecisionLogVO, TBBomFileDecisionLogDM>();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 功能說明：將 EsFileTransferUpload ProcessStatus 代碼轉為中文描述。
        /// </summary>
        /// <param name="code">輸入參數：處理狀態整數代碼（可為 null）。</param>
        /// <returns>輸出參數：enum 描述文字；未知代碼回傳數字字串；null 回傳空字串。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：EsFileTransferUploadProcessStatusEnum.GetDescription。
        /// 訊息內容及生成條件：無 HTTP 回應。
        /// </remarks>
        private static string GetProcessStatusText(int? code)
        {
            return code switch
            {
                (int)EsFileTransferUploadProcessStatusEnum.Pending => EsFileTransferUploadProcessStatusEnum.Pending.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.Transferred => EsFileTransferUploadProcessStatusEnum.Transferred.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.PendingPricingSearch => EsFileTransferUploadProcessStatusEnum.PendingPricingSearch.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.PricingDone => EsFileTransferUploadProcessStatusEnum.PricingDone.GetDescription(),
                _ => code?.ToString() ?? string.Empty,
            };
        }
    }

    public partial class QuotationResultController
    {
        #region -- 查詢 --

        /// <summary>
        /// 功能說明：分頁取得定時查價結果（BOM 上傳檔）清單。
        /// </summary>
        /// <param name="filter">輸入參數：分頁、排序、KeywordLike（QuotationResultSearchVM）。</param>
        /// <returns>輸出參數：JsonSuccess({ Data, Total, Page, PageSize })。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetPageEntity、GetBlEsFileTransferUpload().GetPageListQuotationResult、GetProcessStatusText。
        /// 訊息內容及生成條件：成功 → 清單 JSON；例外 → JsonValidFail(System_Error)。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("GetPageList")]
        public ActionResult GetPageList([FromQuery] QuotationResultSearchVM filter)
        {
            try
            {
                PageEntity pageEntity = GetPageEntity<QuotationFileGridVM>(filter);

                SearchVO searchVO = new();
                searchVO.KeywordLike = filter.KeywordLike;

                PageResult<EsFileTransferUploadDM> pageResult = GetBlEsFileTransferUpload().GetPageListQuotationResult(pageEntity, searchVO);
                int baseNo = (filter.Page - 1) * filter.PageSize;

                List<QuotationFileGridVM> list = new();
                int no = 0;
                foreach (EsFileTransferUploadDM d in pageResult.Results)
                {
                    QuotationFileGridVM vm = new();
                    vm.No = baseNo + no + 1;
                    vm.Id = d.Id;
                    vm.FileName = d.FileName ?? string.Empty;
                    vm.CustomerCode = d.CustomerCode;
                    vm.CustomerName = string.IsNullOrEmpty(vm.CustomerCode) ? d.ManualCustomerName : d.CustomerName;
                    vm.ProdNo = d.ProdNo;
                    vm.QuotationQty = d.QuotationQty ?? 0;
                    vm.ItemCount = d.ItemCount;
                    vm.ProcessStatus = d.ProcessStatus ?? (int)EsFileTransferUploadProcessStatusEnum.Pending;
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
        /// 功能說明：依 Id 取得單筆查價結果詳細（表頭 + BOM 料項 + Mouser/DigiKey 現貨價）。
        /// </summary>
        /// <param name="id">輸入參數：EsFileTransferUpload 主鍵 Guid。</param>
        /// <returns>輸出參數：JsonSuccess(QuotationFileEditVM)；資料不存在時 JsonValidFail「資料不存在」。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetOneForEditQuotationResult、GetBlBomFileContent、GetBlTBBomFileQuotationOther、GetBlTBSysSetting（AI 開關）。
        /// 訊息內容及生成條件：dm==null →「資料不存在」；成功 → 完整 EditVM；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("GetById")]
        public ActionResult GetById(Guid id)
        {
            try
            {
                EsFileTransferUploadDM? dm = GetBlEsFileTransferUpload().GetOneForEditQuotationResult(id);
                if (dm == null)
                    return JsonValidFail("資料不存在");

                QuotationFileEditVM vm = new();
                vm.Id = dm.Id;
                vm.FileName = dm.FileName ?? string.Empty;
                vm.CustomerCode = dm.CustomerCode;
                vm.CustomerName = string.IsNullOrEmpty(vm.CustomerCode) ? dm.ManualCustomerName : dm.CustomerName;
                vm.ProdNo = dm.ProdNo;
                vm.QuotationQty = dm.QuotationQty;
                vm.CreatedAtText = dm.CreatedAt?.ToString("yyyy/MM/dd HH:mm") ?? string.Empty;
                vm.UpdatedAtText = dm.UpdatedAt?.ToString("yyyy/MM/dd HH:mm") ?? string.Empty;
                vm.Items = new();

                // AI 決策過程顯示開關
                List<TBSysSettingDM> sysSettings = GetBlTBSysSetting().GetListByType(new SearchVO(), ParameterTypeEnum.AIDecisionProcessDisplaySwitch.ToString());
                vm.IsAIDecisionProcessDisplay = sysSettings
                    .Select(x => x.Value == "1")
                    .FirstOrDefault(true);

                SearchVO contentSearchVO = new();
                contentSearchVO.UploadIdEq = dm.UploadId;
                List<BomFileContentDM> contentList = GetBlBomFileContent().GetListWithQuotationByFilter(contentSearchVO);

                // 批次取得現貨優惠價結果（Mouser / DigiKey）
                List<Guid> contentIds = contentList
                    .Where(c => c.Id != Guid.Empty)
                    .Select(c => c.Id)
                    .ToList();

                Dictionary<Guid, TBBomFileQuotationOtherDM?> mouserMap = new();
                Dictionary<Guid, TBBomFileQuotationOtherDM?> dkMap = new();

                if (contentIds.Count > 0)
                {
                    SearchVO otherSearchVO = new();
                    otherSearchVO.BomFileContentIdIn = contentIds;
                    List<TBBomFileQuotationOtherDM> otherList = GetBlTBBomFileQuotationOther().GetListByFilter(otherSearchVO);

                    foreach (TBBomFileQuotationOtherDM other in otherList)
                    {
                        if (other.SourceType == (int)BomFileQuotationOtherSourceTypeEnum.Mouser)
                            mouserMap[other.BomFileContentId] = other;
                        else if (other.SourceType == (int)BomFileQuotationOtherSourceTypeEnum.DigiKey)
                            dkMap[other.BomFileContentId] = other;
                    }
                }

                foreach (BomFileContentDM contentDm in contentList)
                {
                    QuotationItemVM itemVm = _mapper.Map<QuotationItemVM>(contentDm);

                    if (contentDm.MatchCategory.HasValue)
                        itemVm.MatchCategoryText = ((MatchCategoryEnum)contentDm.MatchCategory.Value).GetDescription();
                    if (contentDm.ExternalScenario.HasValue)
                        itemVm.ExternalScenarioText = ((ExternalScenarioEnum)contentDm.ExternalScenario.Value).GetDescription();
                    itemVm.IsFilterByCustomerApprovedPartText = contentDm.IsFilterByCustomerApprovedPart ? "是" : "否";

                    if (mouserMap.TryGetValue(contentDm.Id, out TBBomFileQuotationOtherDM? mouserDm) && mouserDm != null)
                    {
                        itemVm.MouserQuotationDate = mouserDm.QuotationDate?.ToString("yyyy/MM/dd");
                        itemVm.MouserUnitPriceOriginalCurrency = mouserDm.UnitPriceOriginalCurrency;
                        itemVm.MouserUnitPriceTwd = mouserDm.UnitPriceTwd;
                        itemVm.MouserMoq = mouserDm.Moq;
                        itemVm.MouserCurrency = mouserDm.Currency;
                        itemVm.MouserSupplierName = mouserDm.SupplierName;
                    }

                    if (dkMap.TryGetValue(contentDm.Id, out TBBomFileQuotationOtherDM? dkDm) && dkDm != null)
                    {
                        itemVm.DkQuotationDate = dkDm.QuotationDate?.ToString("yyyy/MM/dd");
                        itemVm.DkUnitPriceOriginalCurrency = dkDm.UnitPriceOriginalCurrency;
                        itemVm.DkUnitPriceTwd = dkDm.UnitPriceTwd;
                        itemVm.DkMoq = dkDm.Moq;
                        itemVm.DkCurrency = dkDm.Currency;
                        itemVm.DkSupplierName = dkDm.SupplierName;
                    }

                    vm.Items.Add(itemVm);
                }

                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion

        #region -- 操作 --

        /// <summary>
        /// 功能說明：重新內部查價；更新採購型號後執行 RunInternalAsync 並寫入 DB。
        /// </summary>
        /// <param name="request">輸入參數：BomFileContentId、No（採購型號）。</param>
        /// <returns>輸出參數：JsonOK()；資料不存在或例外時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：DoUpdateNo、RunInternalAsync、DoSaveSingleInternalQuotationResult。
        /// 訊息內容及生成條件：content==null →「資料不存在」；成功 → JsonOK；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_Edit)]
        [HttpPost("ReInternalQuotation")]
        public async Task<ActionResult> ReInternalQuotation([FromBody] QuotationReInternalQuotationRequestVM request)
        {
            try
            {
                // 更新採購型號，同時將 IsRecommendedNo 設為 false（使用者確認料號，非建議）
                GetBlTBBomFileQuotation().DoUpdateNo(request.BomFileContentId, request.No, false);

                // 載入料項 DM
                SearchVO contentSearchVO = new();
                contentSearchVO.IdEq = request.BomFileContentId;
                contentSearchVO.IsLimit1 = true;
                BomFileContentDM? content = GetBlBomFileContent().GetListWithQuotationByFilter(contentSearchVO).FirstOrDefault();
                if (content == null)
                    return JsonValidFail("資料不存在");

                // 設為非建議料號，避免被內部查價跳過邏輯略過
                content.IsRecommendedNo = false;

                // 取得客戶代碼
                EsFileTransferUploadDM? upload = GetBlEsFileTransferUpload().GetOneInfo(content.UploadId);
                string? customerCode = upload?.CustomerCode;

                _quotationHandler.ApplyDecisionLogSettingFromSysSetting();
                await _quotationHandler.RunInternalAsync(content, customerCode);

                GetBlHandleQuotation().DoSaveSingleInternalQuotationResult(content);

                return JsonOK();
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 功能說明：重新外部查價（Nexar），依上傳檔報價數量執行並儲存結果。
        /// </summary>
        /// <param name="request">輸入參數：BomFileContentId。</param>
        /// <returns>輸出參數：JsonOK()；失敗時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：AuthorizeExternalAsync、RunExternalAsync、DoSaveSingleExternalQuotationResult。
        /// 訊息內容及生成條件：料項不存在 →「資料不存在」；成功 → JsonOK；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_Edit)]
        [HttpPost("ReExternalQuotation")]
        public async Task<ActionResult> ReExternalQuotation([FromBody] QuotationReExternalQuotationRequestVM request)
        {
            try
            {
                // 載入料項 DM
                SearchVO contentSearchVO = new();
                contentSearchVO.IdEq = request.BomFileContentId;
                contentSearchVO.IsLimit1 = true;
                BomFileContentDM? dmBomFileContent = GetBlBomFileContent().GetListWithQuotationByFilter(contentSearchVO).FirstOrDefault();
                if (dmBomFileContent == null)
                    return JsonValidFail("資料不存在");

                // 取得報價數量
                EsFileTransferUploadDM? dmEsFileTransferUpload = GetBlEsFileTransferUpload().GetOneInfoByUploadId(dmBomFileContent.UploadId);
                int quotationQty = dmEsFileTransferUpload?.QuotationQty ?? 1;

                await _quotationHandler.AuthorizeExternalAsync();
                _quotationHandler.ApplyDecisionLogSettingFromSysSetting();
                await _quotationHandler.RunExternalAsync(dmBomFileContent, quotationQty);

                GetBlHandleQuotation().DoSaveSingleExternalQuotationResult(dmBomFileContent);

                return JsonOK();
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 功能說明：查詢現貨優惠價（並行 Mouser + DigiKey），結果寫入 TBBomFileQuotationOther 與決策歷程。
        /// </summary>
        /// <param name="request">輸入參數：BomFileContentId。</param>
        /// <returns>輸出參數：JsonOK()。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：QueryMouserAction、QueryDkAction、DoSaveSingleInStockPriceResult。
        /// 訊息內容及生成條件：content==null →「資料不存在」；成功 → JsonOK；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_Edit)]
        [HttpPost("CheckInStockPrice")]
        public async Task<ActionResult> CheckInStockPrice([FromBody] QuotationCheckInStockPriceRequestVM request)
        {
            try
            {
                // 載入料項 DM
                SearchVO contentSearchVO = new();
                contentSearchVO.IdEq = request.BomFileContentId;
                contentSearchVO.IsLimit1 = true;
                BomFileContentDM? content = GetBlBomFileContent().GetListWithQuotationByFilter(contentSearchVO).FirstOrDefault();
                if (content == null)
                    return JsonValidFail("資料不存在");

                // 取得報價數量
                EsFileTransferUploadDM? upload = GetBlEsFileTransferUpload().GetOneInfo(content.UploadId);
                int quotationQty = upload?.QuotationQty ?? 1;

                ExternalQueryExecuteHandler.QueryMouserCartPriceReqVO apiReq = new();
                apiReq.PartNumber = content.ManufacturerPartNumber ?? string.Empty;
                apiReq.Quantity = quotationQty;

                // 並行呼叫 Mouser 與 DigiKey
                Task<Const.ApiModels.QueryPrice.QueryActionResultRspVO> mouserTask =
                    _externalQueryExecuteHandler.QueryMouserAction(apiReq);
                Task<Const.ApiModels.QueryPrice.QueryActionResultRspVO> dkTask =
                    _externalQueryExecuteHandler.QueryDkAction(apiReq);

                await Task.WhenAll(mouserTask, dkTask);

                TBBomFileQuotationOtherDM? mouserDm = mouserTask.Result.Result != null
                    ? _mapper.Map<TBBomFileQuotationOtherDM>(mouserTask.Result.Result)
                    : null;
                TBBomFileQuotationOtherDM? dkDm = dkTask.Result.Result != null
                    ? _mapper.Map<TBBomFileQuotationOtherDM>(dkTask.Result.Result)
                    : null;

                List<TBBomFileDecisionLogDM> decisionLogs = new();
                decisionLogs.AddRange(_mapper.Map<List<TBBomFileDecisionLogDM>>(mouserTask.Result.DecisionLogs));
                decisionLogs.AddRange(_mapper.Map<List<TBBomFileDecisionLogDM>>(dkTask.Result.DecisionLogs));

                GetBlHandleQuotation().DoSaveSingleInStockPriceResult(
                    request.BomFileContentId,
                    mouserDm,
                    dkDm,
                    decisionLogs);

                return JsonOK();
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion
    }

    public partial class QuotationResultController
    {
        #region -- 料號快查 --

        /// <summary>
        /// 功能說明：取得客戶清單（快查 Modal 下拉選單）。
        /// </summary>
        /// <returns>輸出參數：JsonSuccess(List&lt;SelectItemVO&gt;)。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetBlReportItemCustomer().GetListEnabled，依 CustomerCode 群組。
        /// 訊息內容及生成條件：成功 → 客戶選項；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("GetCustomerList")]
        public ActionResult GetCustomerList()
        {
            try
            {
                List<SelectItemVO> customers = GetBlReportItemCustomer()
                    .GetListEnabled(new SearchVO())
                    .GroupBy(x => x.CustomerCode)
                    .Select(x => new SelectItemVO(x.First().CustomerName ?? string.Empty, x.Key ?? string.Empty))
                    .ToList();

                return JsonSuccess(customers);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 功能說明：料號快查步驟一，僅查料（RunPartSearchAsync），不寫入 DB。
        /// </summary>
        /// <param name="request">輸入參數：製造商料號、廠牌、元件料號、描述等。</param>
        /// <returns>輸出參數：JsonSuccess({ No, MatchCategoryText, MatchField, DecisionLogs })。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetBrandComparisonCategorySet、RunPartSearchAsync。
        /// 訊息內容及生成條件：成功 → 查料結果與 PendingDecisionLogs；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("QuickPartMatch")]
        public async Task<ActionResult> QuickPartMatch([FromQuery] QuotationQuickPartMatchRequestVM request)
        {
            try
            {
                BomFileContentDM content = new();
                content.ManufacturerPartNumber = request.ManufacturerPartNumber?.Trim();
                content.Manufacturer = request.Manufacturer?.Trim();
                content.ComponentPart = request.ComponentPart?.Trim();
                content.Description = request.Description?.Trim();

                HashSet<string> brandComparisonCategorySet = _quotationHandler.GetBrandComparisonCategorySet();
                await _quotationHandler.RunPartSearchAsync(content, brandComparisonCategorySet);

                string matchCategoryText = content.MatchCategory.HasValue
                    ? ((MatchCategoryEnum)content.MatchCategory.Value).GetDescription()
                    : string.Empty;

                var decisionLogs = content.PendingDecisionLogs
                    .OrderBy(x => x.Stage)
                    .ThenBy(x => x.Step)
                    .Select(x => new
                    {
                        Stage = x.Stage,
                        StageText = x.Stage.HasValue
                            ? ((BomFileDecisionLogStageEnum)x.Stage.Value).GetDescription()
                            : string.Empty,
                        Step = x.Step,
                        StepText = x.Step.HasValue
                            ? ((BomFileDecisionLogStepEnum)x.Step.Value).GetDescription()
                            : string.Empty,
                        Message = x.Message ?? string.Empty
                    }).ToList();

                return JsonSuccess(new
                {
                    No = content.No ?? string.Empty,
                    MatchCategoryText = matchCategoryText,
                    MatchField = content.MatchField ?? string.Empty,
                    DecisionLogs = decisionLogs
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 功能說明：料號快查步驟二，依勾選執行內部/外部/現貨查價，不寫入 DB。
        /// </summary>
        /// <param name="request">輸入參數：RunInternal/RunExternal/RunInStock、料號、客戶、數量等。</param>
        /// <returns>輸出參數：JsonSuccess({ Internal, External, InStock, ClusterMeta, DecisionLogs })。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：RunInternalAsync、RunExternalAsync、QueryMouserAction、QueryDkAction。
        /// 訊息內容及生成條件：成功 → 各區塊結果物件；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("QuickPricing")]
        public async Task<ActionResult> QuickPricing([FromQuery] QuotationQuickPricingRequestVM request)
        {
            try
            {
                object? internalResult = null;
                object? externalResult = null;
                object? inStockResult = null;
                object? clusterMeta = null;
                List<object> decisionLogs = new();

                // ── 內部查價 ──────────────────────────────────────────────
                if (request.RunInternal)
                {
                    BomFileContentDM contentInternal = new();
                    contentInternal.No = request.No?.Trim();
                    contentInternal.ManufacturerPartNumber = request.ManufacturerPartNumber?.Trim();

                    string? customerCode = string.IsNullOrWhiteSpace(request.CustomerCode) ? null : request.CustomerCode.Trim();
                    await _quotationHandler.RunInternalAsync(contentInternal, customerCode);

                    QuotationItemVM itemVm = _mapper.Map<QuotationItemVM>(contentInternal);
                    itemVm.IsFilterByCustomerApprovedPartText = contentInternal.IsFilterByCustomerApprovedPart ? "是" : "否";

                    internalResult = new
                    {
                        InternalPurchaseOrderDate = itemVm.InternalPurchaseOrderDate,
                        InternalUnitPriceOriginalCurrency = itemVm.InternalUnitPriceOriginalCurrency,
                        InternalUnitPriceTwd = itemVm.InternalUnitPriceTwd,
                        InternalQuantity = itemVm.InternalQuantity,
                        InternalCurrency = itemVm.InternalCurrency,
                        InternalSupplierName = itemVm.InternalSupplierName,
                        InternalSupplierCode = itemVm.InternalSupplierCode,
                        InternalItemDescription2 = itemVm.InternalItemDescription2,
                        IsFilterByCustomerApprovedPartText = itemVm.IsFilterByCustomerApprovedPartText
                    };

                    if (!string.IsNullOrWhiteSpace(contentInternal.No))
                    {
                        clusterMeta = new
                        {
                            No = contentInternal.No,
                            CustomerApprovedPartCsv = contentInternal.CustomerApprovedPartCsv ?? string.Empty,
                            LowMinPrice = contentInternal.InternalLowMinPrice,
                            LowMaxPrice = contentInternal.InternalLowMaxPrice,
                            HighMinPrice = contentInternal.InternalHighMinPrice,
                            HighMaxPrice = contentInternal.InternalHighMaxPrice
                        };
                    }

                    decisionLogs.AddRange(contentInternal.PendingDecisionLogs
                        .OrderBy(x => x.Stage).ThenBy(x => x.Step)
                        .Select(x => (object)new
                        {
                            Stage = x.Stage,
                            StageText = x.Stage.HasValue ? ((BomFileDecisionLogStageEnum)x.Stage.Value).GetDescription() : string.Empty,
                            Step = x.Step,
                            StepText = x.Step.HasValue ? ((BomFileDecisionLogStepEnum)x.Step.Value).GetDescription() : string.Empty,
                            Message = x.Message ?? string.Empty
                        }));
                }

                // ── 外部查價 ──────────────────────────────────────────────
                if (request.RunExternal)
                {
                    BomFileContentDM contentExternal = new();
                    contentExternal.ManufacturerPartNumber = request.ManufacturerPartNumber?.Trim();
                    contentExternal.Qty = 1;

                    await _quotationHandler.AuthorizeExternalAsync();
                    await _quotationHandler.RunExternalAsync(contentExternal, request.PurchaseQty);

                    QuotationItemVM extVm = _mapper.Map<QuotationItemVM>(contentExternal);
                    if (contentExternal.ExternalScenario.HasValue)
                        extVm.ExternalScenarioText = ((ExternalScenarioEnum)contentExternal.ExternalScenario.Value).GetDescription();

                    externalResult = new
                    {
                        ExternalQuotationDate = extVm.ExternalQuotationDate,
                        ExternalUnitPriceOriginalCurrency = extVm.ExternalUnitPriceOriginalCurrency,
                        ExternalUnitPriceTwd = extVm.ExternalUnitPriceTwd,
                        ExternalMoq = extVm.ExternalMoq,
                        ExternalCurrency = extVm.ExternalCurrency,
                        ExternalSupplierName = extVm.ExternalSupplierName,
                        ExternalStock = extVm.ExternalStock,
                        ExternalScenarioText = extVm.ExternalScenarioText
                    };

                    decisionLogs.AddRange(contentExternal.PendingDecisionLogs
                        .OrderBy(x => x.Stage).ThenBy(x => x.Step)
                        .Select(x => (object)new
                        {
                            Stage = x.Stage,
                            StageText = x.Stage.HasValue ? ((BomFileDecisionLogStageEnum)x.Stage.Value).GetDescription() : string.Empty,
                            Step = x.Step,
                            StepText = x.Step.HasValue ? ((BomFileDecisionLogStepEnum)x.Step.Value).GetDescription() : string.Empty,
                            Message = x.Message ?? string.Empty
                        }));
                }

                // ── 現貨/DigiKey 查價 ─────────────────────────────────────
                if (request.RunInStock)
                {
                    ExternalQueryExecuteHandler.QueryMouserCartPriceReqVO apiReq = new();
                    apiReq.PartNumber = request.ManufacturerPartNumber?.Trim() ?? string.Empty;
                    apiReq.Quantity = request.PurchaseQty;

                    Task<Const.ApiModels.QueryPrice.QueryActionResultRspVO> mouserTask =
                        _externalQueryExecuteHandler.QueryMouserAction(apiReq);
                    Task<Const.ApiModels.QueryPrice.QueryActionResultRspVO> dkTask =
                        _externalQueryExecuteHandler.QueryDkAction(apiReq);
                    await Task.WhenAll(mouserTask, dkTask);

                    decimal? mouserPrice = mouserTask.Result.Result?.UnitPriceOriginalCurrency;
                    string? mouserDate = mouserTask.Result.Result?.QuotationDate?.ToString("yyyy/MM/dd");
                    decimal? dkPrice = dkTask.Result.Result?.UnitPriceOriginalCurrency;
                    string? dkDate = dkTask.Result.Result?.QuotationDate?.ToString("yyyy/MM/dd");

                    inStockResult = new
                    {
                        MouserQuotationDate = mouserDate,
                        MouserUnitPriceOriginalCurrency = mouserPrice,
                        DkQuotationDate = dkDate,
                        DkUnitPriceOriginalCurrency = dkPrice
                    };

                    List<TBBomFileDecisionLogDM> inStockLogs = new();
                    inStockLogs.AddRange(_mapper.Map<List<TBBomFileDecisionLogDM>>(mouserTask.Result.DecisionLogs));
                    inStockLogs.AddRange(_mapper.Map<List<TBBomFileDecisionLogDM>>(dkTask.Result.DecisionLogs));
                    decisionLogs.AddRange(inStockLogs
                        .OrderBy(x => x.Stage).ThenBy(x => x.Step)
                        .Select(x => (object)new
                        {
                            Stage = x.Stage,
                            StageText = x.Stage.HasValue ? ((BomFileDecisionLogStageEnum)x.Stage.Value).GetDescription() : string.Empty,
                            Step = x.Step,
                            StepText = x.Step.HasValue ? ((BomFileDecisionLogStepEnum)x.Step.Value).GetDescription() : string.Empty,
                            Message = x.Message ?? string.Empty
                        }));
                }

                return JsonSuccess(new
                {
                    Internal = internalResult,
                    External = externalResult,
                    InStock = inStockResult,
                    ClusterMeta = clusterMeta,
                    DecisionLogs = decisionLogs
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion
    }

    public partial class QuotationResultController
    {
        #region -- 決策歷程 --

        /// <summary>
        /// 功能說明：取得指定 BOM 料項的 AI/查價決策歷程清單。
        /// </summary>
        /// <param name="bomFileContentId">輸入參數：BomFileContent 主鍵 Guid。</param>
        /// <returns>輸出參數：JsonSuccess(決策歷程陣列，含 StageText/StepText/Message)。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetBlTBBomFileDecisionLog().GetListEnabled。
        /// 訊息內容及生成條件：成功 → 依 Stage/Step 排序的清單；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("GetDecisionLogs")]
        public ActionResult GetDecisionLogs(Guid bomFileContentId)
        {
            try
            {
                SearchVO searchVO = new();
                searchVO.BomFileContentIdEq = bomFileContentId;
                List<TBBomFileDecisionLogDM> logs = GetBlTBBomFileDecisionLog().GetListEnabled(searchVO)
                    .OrderBy(x => x.Stage)
                    .ThenBy(x => x.Step)
                    .ToList();

                var result = logs.Select(x => new
                {
                    Stage = x.Stage,
                    StageText = x.Stage.HasValue
                        ? ((BomFileDecisionLogStageEnum)x.Stage.Value).GetDescription()
                        : string.Empty,
                    Step = x.Step,
                    StepText = x.Step.HasValue
                        ? ((BomFileDecisionLogStepEnum)x.Step.Value).GetDescription()
                        : string.Empty,
                    Message = x.Message ?? string.Empty
                }).ToList();

                return JsonSuccess(result);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 功能說明：取得價格分群採購紀錄（分頁）；支援 bomFileContentId 或快查 No 模式。
        /// </summary>
        /// <param name="bomFileContentId">輸入參數：料項 Id（與 no 二擇一）。</param>
        /// <param name="no">輸入參數：採購型號（快查模式）。</param>
        /// <param name="customerApprovedPartCsv">輸入參數：客戶認可料號 CSV。</param>
        /// <param name="lowMinPrice">輸入參數：低價群最小值（快查模式）。</param>
        /// <param name="lowMaxPrice">輸入參數：低價群最大值。</param>
        /// <param name="highMinPrice">輸入參數：高價群最小值。</param>
        /// <param name="highMaxPrice">輸入參數：高價群最大值。</param>
        /// <param name="page">輸入參數：頁碼，預設 1。</param>
        /// <param name="pageSize">輸入參數：每頁筆數，預設 20。</param>
        /// <returns>輸出參數：JsonSuccess({ ClusterRanges, Records, Total })；quotation 為 null 時 Total=0。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetBlTBBomFileQuotation、GetBlSanderModulePurchaseLine().GetPageListPriceCluster。
        /// 訊息內容及生成條件：有 bomFileContentId 但無 quotation → 空 Records；成功 → 分群範圍與採購明細；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("GetPriceClusterPageList")]
        public ActionResult GetPriceClusterPageList(
            Guid? bomFileContentId,
            string? no,
            string? customerApprovedPartCsv,
            decimal? lowMinPrice,
            decimal? lowMaxPrice,
            decimal? highMinPrice,
            decimal? highMaxPrice,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                string? resolvedNo = no;
                string? resolvedCsv = customerApprovedPartCsv;
                object? clusterRanges;

                if (bomFileContentId.HasValue)
                {
                    TBBomFileQuotationDM? quotation = GetBlTBBomFileQuotation().GetOneByBomFileContentId(bomFileContentId.Value);
                    if (quotation == null)
                        return JsonSuccess(new { ClusterRanges = (object?)null, Records = Array.Empty<object>(), Total = 0 });

                    resolvedNo = quotation.No;
                    resolvedCsv = quotation.CustomerApprovedPartCsv;
                    clusterRanges = new
                    {
                        LowMinPrice = quotation.InternalLowMinPrice,
                        LowMaxPrice = quotation.InternalLowMaxPrice,
                        HighMinPrice = quotation.InternalHighMinPrice,
                        HighMaxPrice = quotation.InternalHighMaxPrice
                    };
                }
                else
                {
                    clusterRanges = new
                    {
                        LowMinPrice = lowMinPrice,
                        LowMaxPrice = lowMaxPrice,
                        HighMinPrice = highMinPrice,
                        HighMaxPrice = highMaxPrice
                    };
                }

                SearchVO searchVO = new();
                searchVO.UnitCostLcyGt = 0;
                if (!string.IsNullOrWhiteSpace(resolvedNo))
                    searchVO.SanderModuleItemNoEq = resolvedNo;
                if (!string.IsNullOrEmpty(resolvedCsv))
                    searchVO.Description2In = resolvedCsv.Split(',').ToList();

                PageEntity pageEntity = new();
                pageEntity.CurrentPage = page;
                pageEntity.PageDataSize = pageSize;

                PageResult<SanderModulePurchaseLineDM> pageResult = GetBlSanderModulePurchaseLine().GetPageListPriceCluster(pageEntity, searchVO);

                var records = pageResult.Results.Select(x => new
                {
                    DocumentDate = x.DocumentDate?.ToString("yyyy/MM/dd"),
                    Description2 = x.Description2 ?? string.Empty,
                    BuyFromVendorName = x.BuyFromVendorName ?? string.Empty,
                    UnitCost = x.UnitCost,
                    UnitCostLcy = x.UnitCostLcy,
                    Quantity = x.Quantity,
                    CurrencyCode = x.CurrencyCode ?? string.Empty
                }).ToList();

                return JsonSuccess(new { ClusterRanges = clusterRanges, Records = records, Total = pageResult.DataCount });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        #endregion
    }

    public partial class QuotationResultController
    {
        #region -- 匯出 --

        /// <summary>匯出時附加於原始 Excel 右側的擴充標題欄位（依 Edit.cshtml table-responsive 欄位順序）</summary>
        private static readonly string[] ExportExtraHeaders =
        {
            "內部料號", "比對結果分類", "比對命中欄位", "內部價格(原幣)", "幣別(內部)", "供應商（內部）",
            "單據日期", "採購型號", "外部價格(原幣)", "幣別(外部)", "供應商（外部）", "庫存量", "MOQ 階梯", "情境標註",
            "查價日期(Mouser)", "優惠價(原幣)(Mouser)", "查價日期(DigiKey)", "優惠價(原幣)(DigiKey)",
        };

        /// <summary>擴充欄位中需隱藏的相對索引（比照畫面：內部／外部幣別不顯示，資料仍寫入）</summary>
        private static readonly int[] ExportHiddenExtraHeaderIndexes = [4, 9];

        /// <summary>
        /// 功能說明：匯出現貨優惠價查詢結果；讀取原始上傳 Excel，於標題列最後一個有效欄位右側附加系統比對資料後下載。
        /// </summary>
        /// <param name="id">輸入參數：EsFileTransferUpload 主鍵 Guid。</param>
        /// <returns>輸出參數：附加擴充欄位後的 Excel 檔案（attachment）；驗證失敗回傳 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：PathProvider.EsFileTransferUpload（原始檔）、TableExcelBL.GetOne（匯入規則含欄位明細，status=1）、GetListWithQuotationByFilter（查價結果）。
        /// 訊息內容及生成條件：資料/檔案/規則不存在 → JsonValidFail 對應訊息；成功 → File 下載；例外 → System_Error。
        /// </remarks>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("Export")]
        public ActionResult Export(Guid id)
        {
            try
            {
                EsFileTransferUploadDM? upload = GetBlEsFileTransferUpload().GetOneInfo(id);
                if (upload == null)
                    return JsonValidFail("資料不存在");
                if (!upload.EsFileTransferMappingId.HasValue)
                    return JsonValidFail("此檔案未設定匯入規則，無法匯出");

                // 原始上傳檔：檔名 = uploadid + 原始檔名副檔名
                string ext = Path.GetExtension(upload.FileName ?? string.Empty);
                string filePath = Path.Combine(_pathProvider.EsFileTransferUpload, upload.UploadId + ext);
                if (!System.IO.File.Exists(filePath))
                    return JsonValidFail("原始上傳檔案不存在，無法匯出");

                // 匯入規則欄位設定（EsFileTransferMappingColumn，DAO 內已過濾 status=1）
                EsFileTransferMappingDM? mapping = GetBLInstance<TableExcelBL>().GetOne(upload.EsFileTransferMappingId.Value);
                if (mapping == null || mapping.Columns.Count == 0)
                    return JsonValidFail("找不到匯入規則欄位設定，無法匯出");

                // BOM 料項對應的欄位群（同一工作表 HeaderRowIndex 相同）；無 bomfilecontent 設定時退回全部欄位
                List<EsFileTransferMappingColumnDM> bomColumns = mapping.Columns
                    .Where(c => string.Equals(c.TargetTableName, "bomfilecontent", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (bomColumns.Count == 0)
                    bomColumns = mapping.Columns;

                int sheetIndex = bomColumns[0].SrcSheetIndex;
                string sheetName = bomColumns[0].SrcSheetName ?? string.Empty;
                int headerRowIndex = bomColumns[0].HeaderRowIndex - 1; // HeaderRowIndex 為 1-based

                // 查價結果（同 GetById 的 contentList）
                SearchVO contentSearchVO = new();
                contentSearchVO.UploadIdEq = upload.UploadId;
                List<BomFileContentDM> contentList = GetBlBomFileContent().GetListWithQuotationByFilter(contentSearchVO);

                // 批次取得現貨優惠價（Mouser / DigiKey）
                Dictionary<Guid, TBBomFileQuotationOtherDM> mouserMap = new();
                Dictionary<Guid, TBBomFileQuotationOtherDM> dkMap = new();
                List<Guid> contentIds = contentList
                    .Where(c => c.Id != Guid.Empty)
                    .Select(c => c.Id)
                    .ToList();
                if (contentIds.Count > 0)
                {
                    SearchVO otherSearchVO = new();
                    otherSearchVO.BomFileContentIdIn = contentIds;
                    List<TBBomFileQuotationOtherDM> otherList = GetBlTBBomFileQuotationOther().GetListByFilter(otherSearchVO);
                    foreach (TBBomFileQuotationOtherDM other in otherList)
                    {
                        if (other.SourceType == (int)BomFileQuotationOtherSourceTypeEnum.Mouser)
                            mouserMap[other.BomFileContentId] = other;
                        else if (other.SourceType == (int)BomFileQuotationOtherSourceTypeEnum.DigiKey)
                            dkMap[other.BomFileContentId] = other;
                    }
                }

                byte[] fileBytes;
                using (FileStream stream = new(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                        ? new XSSFWorkbook(stream)
                        : new HSSFWorkbook(stream);

                    // 依匯入規則選取工作表：優先 SrcSheetName（跨檔案 sheet 順序可能不同），再退回 SrcSheetIndex
                    ISheet? sheet = ResolveExportSheet(workbook, sheetIndex, sheetName);
                    if (sheet == null)
                        return JsonValidFail($"找不到工作表：index={sheetIndex}, name={sheetName}");

                    // 開啟檔案時直接落在有填入查價結果的工作表
                    int resolvedSheetIdx = workbook.GetSheetIndex(sheet);
                    if (resolvedSheetIdx >= 0)
                        workbook.SetActiveSheet(resolvedSheetIdx);

                    IRow? headerRow = sheet.GetRow(headerRowIndex);
                    if (headerRow == null)
                        return JsonValidFail("找不到標題列，無法匯出");

                    // 擴充欄位起點 = 標題列最後一個有文字內容欄位的下一欄
                    int startCol = GetLastTextCellIndex(headerRow) + 1;

                    // 擴充欄位樣式：Arial 字型 + 細格線（標題粗體）
                    IFont headerFont = workbook.CreateFont();
                    headerFont.IsBold = true;
                    headerFont.FontName = "Arial";
                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    headerStyle.SetFont(headerFont);
                    SetThinBorders(headerStyle);

                    IFont dataFont = workbook.CreateFont();
                    dataFont.FontName = "Arial";
                    ICellStyle dataStyle = workbook.CreateCellStyle();
                    dataStyle.SetFont(dataFont);
                    SetThinBorders(dataStyle);

                    // 寫入擴充標題
                    for (int i = 0; i < ExportExtraHeaders.Length; i++)
                    {
                        ICell cell = headerRow.GetCell(startCol + i) ?? headerRow.CreateCell(startCol + i);
                        cell.SetCellValue(ExportExtraHeaders[i]);
                        cell.CellStyle = headerStyle;
                    }

                    // 以匯入規則主鍵欄位（與轉檔 Upsert 一致）將 Excel 資料列對應回查價結果；無主鍵時退回識別欄位
                    Dictionary<string, int> headerMap = BuildHeaderMap(headerRow);
                    List<EsFileTransferMappingColumnDM> pkBomColumns = bomColumns
                        .Where(c => c.IsPrimaryKey
                            && !string.IsNullOrWhiteSpace(c.SrcFileColumnName)
                            && headerMap.ContainsKey(c.SrcFileColumnName)
                            && GetContentKeyValue(new BomFileContentDM(), c.TargetTableColumnName) != KeyFieldNotSupported)
                        .ToList();

                    List<(string TargetCol, int CellIdx, string? DefaultValue)> keyColumns;
                    if (pkBomColumns.Count > 0)
                    {
                        keyColumns = pkBomColumns
                            .Select(c => (c.TargetTableColumnName, headerMap[c.SrcFileColumnName], (string?)c.DefaultValue))
                            .ToList();
                    }
                    else
                    {
                        keyColumns = bomColumns
                            .Where(c => !string.IsNullOrWhiteSpace(c.SrcFileColumnName)
                                && headerMap.ContainsKey(c.SrcFileColumnName)
                                && GetContentKeyValue(new BomFileContentDM(), c.TargetTableColumnName) != KeyFieldNotSupported)
                            .Select(c => (c.TargetTableColumnName, headerMap[c.SrcFileColumnName], (string?)c.DefaultValue))
                            .ToList();
                    }

                    Dictionary<string, Queue<BomFileContentDM>> contentQueues = new();
                    foreach (BomFileContentDM content in contentList)
                    {
                        string key = string.Join("\u001f", keyColumns.Select(k => NormalizeKeyValue(GetContentKeyValue(content, k.TargetCol))));
                        if (!contentQueues.TryGetValue(key, out Queue<BomFileContentDM>? queue))
                        {
                            queue = new Queue<BomFileContentDM>();
                            contentQueues[key] = queue;
                        }
                        queue.Enqueue(content);
                    }

                    // 逐列寫入擴充資料
                    for (int rowIdx = headerRowIndex + 1; rowIdx <= sheet.LastRowNum; rowIdx++)
                    {
                        IRow? row = sheet.GetRow(rowIdx);
                        if (row == null || IsRowEmpty(row)) continue;

                        // 不論是否比對成功，擴充欄位範圍一律先套用樣式（Arial + 格線），維持表格外觀一致
                        for (int i = 0; i < ExportExtraHeaders.Length; i++)
                        {
                            ICell cell = row.GetCell(startCol + i) ?? row.CreateCell(startCol + i);
                            cell.CellStyle = dataStyle;
                        }

                        string rowKey = string.Join("\u001f", keyColumns.Select(k =>
                        {
                            ICell? cell = row.GetCell(k.CellIdx);
                            string? value = cell == null ? null : GetCellValue(cell);
                            if (string.IsNullOrEmpty(value)) value = k.DefaultValue;
                            return NormalizeKeyValue(value);
                        }));

                        if (!contentQueues.TryGetValue(rowKey, out Queue<BomFileContentDM>? matched) || matched.Count == 0)
                            continue;

                        BomFileContentDM matchedContent = matched.Peek();
                        mouserMap.TryGetValue(matchedContent.Id, out TBBomFileQuotationOtherDM? mouserDm);
                        dkMap.TryGetValue(matchedContent.Id, out TBBomFileQuotationOtherDM? dkDm);
                        WriteExportRow(row, startCol, matchedContent, mouserDm, dkDm);
                    }

                    // 擴充欄位欄寬依內容實際長度調整（含標題；全形字以 2 個字元計）
                    AutoFitExportColumns(sheet, headerRowIndex, startCol, ExportExtraHeaders.Length);

                    // 比照查價結果畫面：內部／外部幣別寫入後隱藏欄位
                    foreach (int hiddenOffset in ExportHiddenExtraHeaderIndexes)
                        sheet.SetColumnHidden(startCol + hiddenOffset, true);

                    using MemoryStream ms = new();
                    workbook.Write(ms, true);
                    fileBytes = ms.ToArray();
                }

                string downloadName = $"{Path.GetFileNameWithoutExtension(upload.FileName)}_查價結果{ext}";
                string mimeType = Method.GetMimeType(downloadName);
                string encodedFileName = Uri.EscapeDataString(downloadName);
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}";
                return File(fileBytes, mimeType);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 功能說明：依匯入規則解析要填入查價結果的工作表。優先以 SrcSheetName 名稱比對，找不到再以 SrcSheetIndex。
        /// </summary>
        /// <param name="workbook">輸入參數：Excel 活頁簿。</param>
        /// <param name="sheetIndex">輸入參數：匯入規則 SrcSheetIndex。</param>
        /// <param name="sheetName">輸入參數：匯入規則 SrcSheetName。</param>
        /// <returns>輸出參數：對應工作表；皆找不到時回傳 null。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：與轉檔不同，匯出優先名稱，因同一規則套用到不同上傳檔時 sheet 順序可能改變，名稱較能對到「規則所選 sheet」。
        /// 訊息內容及生成條件：無 HTTP 回應。
        /// </remarks>
        private static ISheet? ResolveExportSheet(IWorkbook workbook, int sheetIndex, string? sheetName)
        {
            if (!string.IsNullOrWhiteSpace(sheetName))
            {
                ISheet? byName = workbook.GetSheet(sheetName.Trim());
                if (byName != null)
                    return byName;
            }

            if (sheetIndex >= 0 && sheetIndex < workbook.NumberOfSheets)
                return workbook.GetSheetAt(sheetIndex);

            return null;
        }

        /// <summary>
        /// 功能說明：將單筆查價結果依擴充欄位順序寫入 Excel 資料列。
        /// </summary>
        /// <param name="row">輸入參數：目標資料列。</param>
        /// <param name="startCol">輸入參數：擴充欄位起始欄索引（0-based）。</param>
        /// <param name="content">輸入參數：查價結果 DM。</param>
        /// <param name="mouser">輸入參數：Mouser 現貨優惠價（可為 null）。</param>
        /// <param name="digiKey">輸入參數：DigiKey 現貨優惠價（可為 null）。</param>
        /// <remarks>
        /// 參考功能名稱與用途：欄位順序對應 ExportExtraHeaders；價格欄位輸出原幣金額（數值），幣別欄位寫入後由 Export 以 SetColumnHidden 隱藏。
        /// 訊息內容及生成條件：無 HTTP 回應。
        /// </remarks>
        private static void WriteExportRow(
            IRow row,
            int startCol,
            BomFileContentDM content,
            TBBomFileQuotationOtherDM? mouser = null,
            TBBomFileQuotationOtherDM? digiKey = null)
        {
            string matchCategoryText = content.MatchCategory.HasValue
                ? ((MatchCategoryEnum)content.MatchCategory.Value).GetDescription()
                : string.Empty;
            string scenarioText = content.ExternalScenario.HasValue
                ? ((ExternalScenarioEnum)content.ExternalScenario.Value).GetDescription()
                : string.Empty;

            // 供應商（內部）：名稱 (代碼)；無代碼則僅顯示名稱
            string internalSupplier = content.InternalSupplierName ?? string.Empty;
            if (!string.IsNullOrEmpty(content.InternalSupplierCode))
                internalSupplier = $"{internalSupplier} ({content.InternalSupplierCode})".Trim();

            int col = startCol;
            SetTextCell(row, col++, content.No);
            SetTextCell(row, col++, matchCategoryText);
            SetTextCell(row, col++, content.MatchField);
            SetNumericCell(row, col++, content.InternalUnitPriceOriginalCurrency);
            SetTextCell(row, col++, content.InternalCurrency);
            SetTextCell(row, col++, internalSupplier);
            SetTextCell(row, col++, content.InternalPurchaseOrderDate?.ToString("yyyy/MM/dd"));
            SetTextCell(row, col++, content.InternalItemDescription2);
            SetNumericCell(row, col++, content.ExternalUnitPriceOriginalCurrency);
            SetTextCell(row, col++, content.ExternalCurrency);
            SetTextCell(row, col++, content.ExternalSupplierName);
            SetNumericCell(row, col++, content.ExternalStock);
            SetNumericCell(row, col++, content.ExternalMoq);
            SetTextCell(row, col++, scenarioText);
            SetTextCell(row, col++, mouser?.QuotationDate?.ToString("yyyy/MM/dd"));
            SetNumericCell(row, col++, mouser?.UnitPriceOriginalCurrency);
            SetTextCell(row, col++, digiKey?.QuotationDate?.ToString("yyyy/MM/dd"));
            SetNumericCell(row, col, digiKey?.UnitPriceOriginalCurrency);
        }

        /// <summary>
        /// 功能說明：依內容實際字數調整擴充欄位的欄寬（從標題列掃描到最後一列，取每欄最長內容）。
        /// </summary>
        /// <param name="sheet">輸入參數：目標工作表。</param>
        /// <param name="headerRowIndex">輸入參數：標題列索引（0-based）。</param>
        /// <param name="startCol">輸入參數：擴充欄位起始欄索引（0-based）。</param>
        /// <param name="columnCount">輸入參數：擴充欄位數。</param>
        /// <remarks>
        /// 參考功能名稱與用途：不使用 AutoSizeColumn（伺服器無對應字型時會失敗），改以字元數計算；全形字計 2 個字元寬。
        /// 訊息內容及生成條件：無 HTTP 回應。
        /// </remarks>
        private static void AutoFitExportColumns(ISheet sheet, int headerRowIndex, int startCol, int columnCount)
        {
            const int maxExcelWidth = 255 * 256; // Excel 欄寬上限（單位：1/256 字元）

            for (int i = 0; i < columnCount; i++)
            {
                int colIdx = startCol + i;
                int maxLen = 0;

                for (int rowIdx = headerRowIndex; rowIdx <= sheet.LastRowNum; rowIdx++)
                {
                    ICell? cell = sheet.GetRow(rowIdx)?.GetCell(colIdx);
                    if (cell == null) continue;
                    string text = GetCellValue(cell) ?? string.Empty;
                    int len = GetDisplayWidth(text);
                    if (len > maxLen) maxLen = len;
                }

                if (maxLen == 0) continue;
                // +2 字元邊距，避免內容貼齊框線
                sheet.SetColumnWidth(colIdx, Math.Min((maxLen + 2) * 256, maxExcelWidth));
            }
        }

        /// <summary>計算字串顯示寬度：ASCII 計 1 個字元，全形（中文等非 Latin-1 字元）計 2 個字元。</summary>
        private static int GetDisplayWidth(string text)
        {
            int width = 0;
            foreach (char c in text)
                width += c > 0xFF ? 2 : 1;
            return width;
        }

        /// <summary>套用上下左右細邊框（擴充欄位格線）。</summary>
        private static void SetThinBorders(ICellStyle style)
        {
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
        }

        private static void SetTextCell(IRow row, int colIdx, string? value)
        {
            ICell cell = row.GetCell(colIdx) ?? row.CreateCell(colIdx);
            cell.SetCellValue(value ?? string.Empty);
        }

        private static void SetNumericCell(IRow row, int colIdx, int? value)
        {
            ICell cell = row.GetCell(colIdx) ?? row.CreateCell(colIdx);
            if (value.HasValue)
                cell.SetCellValue(value.Value);
            else
                cell.SetCellValue(string.Empty);
        }

        private static void SetNumericCell(IRow row, int colIdx, decimal? value)
        {
            ICell cell = row.GetCell(colIdx) ?? row.CreateCell(colIdx);
            if (value.HasValue)
                cell.SetCellValue((double)value.Value);
            else
                cell.SetCellValue(string.Empty);
        }

        /// <summary>識別欄位不支援時的標記值（非六大識別欄位者不納入比對 key）</summary>
        private const string KeyFieldNotSupported = "\u0000__NOT_SUPPORTED__";

        /// <summary>
        /// 功能說明：依匯入規則目標欄位名稱取出查價結果 DM 上對應的識別欄位值，用於 Excel 列與料項的比對 key。
        /// </summary>
        /// <param name="content">輸入參數：查價結果 DM。</param>
        /// <param name="targetColumnName">輸入參數：EsFileTransferMappingColumn.TargetTableColumnName。</param>
        /// <returns>輸出參數：欄位字串值；非識別欄位回傳 KeyFieldNotSupported。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：bomfilecontent 六個識別欄位（ComponentPart/Description/Qty/Manufacturer/ManufacturerPartNumber/DisplayPart）。
        /// 訊息內容及生成條件：無 HTTP 回應。
        /// </remarks>
        private static string? GetContentKeyValue(BomFileContentDM content, string targetColumnName)
        {
            return (targetColumnName ?? string.Empty).ToLowerInvariant() switch
            {
                "componentpart" => content.ComponentPart,
                "description" => content.Description,
                "qty" => content.Qty?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "manufacturer" => content.Manufacturer,
                "manufacturerpartnumber" => content.ManufacturerPartNumber,
                "displaypart" => content.DisplayPart,
                _ => KeyFieldNotSupported,
            };
        }

        /// <summary>比對 key 正規化：去除前後空白；數值字串去除小數點後多餘的 0（Excel 數值欄與 DB 一致化）。</summary>
        private static string NormalizeKeyValue(string? value)
        {
            string trimmed = value?.Trim() ?? string.Empty;
            if (decimal.TryParse(trimmed, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal number))
                return number.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
            return trimmed;
        }

        /// <summary>取得標題列最後一個有文字內容的欄位索引（0-based）；全空時回傳 -1。</summary>
        private static int GetLastTextCellIndex(IRow row)
        {
            int last = -1;
            for (int i = 0; i < row.LastCellNum; i++)
            {
                ICell? cell = row.GetCell(i);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                    last = i;
            }
            return last;
        }

        /// <summary>建立標題文字 → 欄索引對應（忽略大小寫，重複標題取第一個）。</summary>
        private static Dictionary<string, int> BuildHeaderMap(IRow headerRow)
        {
            Dictionary<string, int> map = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                ICell? cell = headerRow.GetCell(i);
                if (cell == null) continue;
                string? title = cell.ToString()?.Trim();
                if (!string.IsNullOrEmpty(title) && !map.ContainsKey(title))
                    map[title] = i;
            }
            return map;
        }

        /// <summary>判斷資料列是否整列為空。</summary>
        private static bool IsRowEmpty(IRow row)
        {
            for (int i = row.FirstCellNum; i < row.LastCellNum; i++)
            {
                ICell? cell = row.GetCell(i);
                if (cell != null && cell.CellType != CellType.Blank && !string.IsNullOrWhiteSpace(cell.ToString()))
                    return false;
            }
            return true;
        }

        /// <summary>讀取儲存格字串值（數值/日期/布林/公式快取結果轉字串，與轉檔邏輯一致）。</summary>
        private static string? GetCellValue(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss")
                    : cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CachedFormulaResultType switch
                {
                    CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                        ? cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss")
                        : cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    _ => cell.StringCellValue
                },
                _ => cell.ToString()?.Trim()
            };
        }

        #endregion
    }
}
