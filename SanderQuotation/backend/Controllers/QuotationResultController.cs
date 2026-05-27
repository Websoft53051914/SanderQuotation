using AutoMapper;
using backend.Common;
using backend.Common.Attribute;
using backend.Models;
using Business.DomainModel;
using Const;
using Const.ApiModels.QueryPrice;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// constructor
        /// </summary>
        public QuotationResultController(
            IConfiguration configuration,
            QuotationHandler quotationHandler,
            ExternalQueryExecuteHandler externalQueryExecuteHandler) : base(configuration)
        {
            _quotationHandler = quotationHandler;
            _externalQueryExecuteHandler = externalQueryExecuteHandler;
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
        /// 將 ProcessStatus 代碼轉換為對應的中文描述文字
        /// </summary>
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
        /// 分頁取得定時查價結果清單
        /// </summary>
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
        /// 依 Id 取得單筆查價結果詳細（Header + BOM 料項）
        /// </summary>
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
        /// 重新內部查價：以前端調整的採購型號執行內部查價
        /// </summary>
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
        /// 重新外部查價：對指定料項重新執行 Nexar 外部查價
        /// </summary>
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
        /// 查詢現貨優惠價：同時呼叫 Mouser 與 DigiKey API 取得現貨價，儲存至 TBBomFileQuotationOther
        /// </summary>
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
        /// 料號快查：依製造商料號在記憶體中執行查料與內部查價，結果不寫入資料庫
        /// </summary>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("QuickSearch")]
        public async Task<ActionResult> QuickSearch([FromQuery] QuotationQuickSearchRequestVM request)
        {
            try
            {
                BomFileContentDM content = new();
                content.ManufacturerPartNumber = request.ManufacturerPartNumber?.Trim();
                content.ComponentPart = request.ComponentPart?.Trim();
                content.Manufacturer = request.Manufacturer?.Trim();

                // 查料：找出內部採購型號（No）
                HashSet<string> brandComparisonCategorySet = _quotationHandler.GetBrandComparisonCategorySet();
                await _quotationHandler.RunPartSearchAsync(content, brandComparisonCategorySet);

                // 內部查價（結果填入 content，不存 DB）
                string? customerCode = string.IsNullOrWhiteSpace(request.CustomerCode) ? null : request.CustomerCode.Trim();
                await _quotationHandler.RunInternalAsync(content, customerCode);

                // 對應至 VM
                QuotationItemVM itemVm = _mapper.Map<QuotationItemVM>(content);
                if (content.MatchCategory.HasValue)
                    itemVm.MatchCategoryText = ((MatchCategoryEnum)content.MatchCategory.Value).GetDescription();
                if (content.ExternalScenario.HasValue)
                    itemVm.ExternalScenarioText = ((ExternalScenarioEnum)content.ExternalScenario.Value).GetDescription();
                itemVm.IsFilterByCustomerApprovedPartText = content.IsFilterByCustomerApprovedPart ? "是" : "否";

                // 決策歷程（來自記憶體，不自 DB 讀取）
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

                // 價格分群資料（以查詢到的 No 重新拉採購歷史 + 使用記憶體中的分群範圍）
                object? clusterData = null;
                if (!string.IsNullOrWhiteSpace(content.No))
                {
                    SearchVO historySearchVO = new();
                    historySearchVO.SanderModuleItemNoEq = content.No;
                    historySearchVO.UnitCostLcyGt = 0;
                    if (!string.IsNullOrEmpty(content.CustomerApprovedPartCsv))
                        historySearchVO.Description2In = content.CustomerApprovedPartCsv.Split(',').ToList();

                    List<SanderModulePurchaseLineDM> purchases = GetBlSanderModulePurchaseLine().GetListByFilter(historySearchVO);

                    clusterData = new
                    {
                        ClusterRanges = new
                        {
                            LowMinPrice = content.InternalLowMinPrice,
                            LowMaxPrice = content.InternalLowMaxPrice,
                            HighMinPrice = content.InternalHighMinPrice,
                            HighMaxPrice = content.InternalHighMaxPrice
                        },
                        Records = purchases.Select(x => new
                        {
                            DocumentDate = x.DocumentDate?.ToString("yyyy/MM/dd"),
                            Description2 = x.Description2 ?? string.Empty,
                            BuyFromVendorName = x.BuyFromVendorName ?? string.Empty,
                            UnitCost = x.UnitCost,
                            UnitCostLcy = x.UnitCostLcy,
                            Quantity = x.Quantity,
                            CurrencyCode = x.CurrencyCode ?? string.Empty
                        }).ToList()
                    };
                }

                return JsonSuccess(new { Item = itemVm, DecisionLogs = decisionLogs, ClusterData = clusterData });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得客戶清單（供快查 Modal 客戶名稱下拉選單使用）
        /// </summary>
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
        /// 快查步驟一：僅執行查料（RunPartSearchAsync），結果不寫入資料庫
        /// </summary>
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
        /// 快查步驟二：依勾選項目執行查價（內部/外部/現貨），結果不寫入資料庫
        /// </summary>
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
        /// 取得指定料項的決策歷程清單
        /// </summary>
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
        /// 取得指定料項的價格分群資料（分頁）—— 支援 bomFileContentId 模式（含 ClusterRanges）或 No 模式（快查）
        /// </summary>
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
}
