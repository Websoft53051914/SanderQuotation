using System.Reflection;
using AutoMapper;
using backend.Common;
using backend.Models;
using Business;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.CustomAttribute;
using CommonClass.Model;
using Const;
using Const.ApiModels.QueryPrice;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using ViewModel;
using ViewModel.QuotationResult;
using static Const.Enums;

namespace backend.Services.QuotationResult;

/// <summary>
/// 定時查價結果應用服務：彙整 BL 查詢、QuotationHandler 查價流程與快查組裝，供 QuotationResultController 呼叫。
/// </summary>
public class QuotationResultAppService : IQuotationResultAppService
{
    private readonly QuotationHandler _quotationHandler;
    private readonly IExternalQueryExecuteHandler _externalQueryExecuteHandler;
    private readonly IMapper _mapper;

    /// <summary>
    /// 功能說明：注入查價處理器與外部 API 執行器，並建立 AutoMapper 對應。
    /// </summary>
    /// <param name="quotationHandler">輸入參數：內部/外部查價與查料流程。</param>
    /// <param name="externalQueryExecuteHandler">輸入參數：Mouser/DigiKey 現貨查價。</param>
    /// <remarks>
    /// 參考功能名稱與用途：BomFileContentDM→QuotationItemVM、QueryActionResultRspVO→TBBomFileQuotationOtherDM/TBBomFileDecisionLogDM。
    /// 訊息內容及生成條件：建構子本身不產生業務訊息。
    /// </remarks>
    public QuotationResultAppService(
        QuotationHandler quotationHandler,
        IExternalQueryExecuteHandler externalQueryExecuteHandler)
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
    /// 功能說明：將 EsFileTransferUpload ProcessStatus 代碼轉為中文描述。
    /// </summary>
    /// <param name="code">輸入參數：處理狀態整數代碼（可為 null）。</param>
    /// <returns>輸出參數：enum 描述文字；未知代碼回傳數字字串；null 回傳空字串。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：EsFileTransferUploadProcessStatusEnum.GetDescription；GetPageList 填入 ProcessStatusText。
    /// 訊息內容及生成條件：無 HTTP/業務錯誤訊息。
    /// </remarks>
    public static string GetProcessStatusText(int? code)
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

    /// <inheritdoc />
    public object GetPageList(QuotationResultSearchVM filter)
    {
        PageEntity pageEntity = BuildPageEntity<QuotationFileGridVM>(filter);

        SearchVO searchVO = new();
        searchVO.KeywordLike = filter.KeywordLike;

        EsFileTransferUploadBL bl = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();
        PageResult<EsFileTransferUploadDM> pageResult = bl.GetPageListQuotationResult(pageEntity, searchVO);
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

        return new
        {
            Data = list,
            Total = pageResult.DataCount,
            Page = pageResult.CurrentPage,
            PageSize = pageResult.PageDataSize,
        };
    }

    /// <inheritdoc />
    public QuotationFileEditVM? GetById(Guid id)
    {
        EsFileTransferUploadBL uploadBl = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();
        EsFileTransferUploadDM? dm = uploadBl.GetOneForEditQuotationResult(id);
        if (dm == null)
            return null;

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

        TBSysSettingBL sysSettingBl = BLFactory.GetInstanceBackGround<TBSysSettingBL>();
        List<TBSysSettingDM> sysSettings = sysSettingBl.GetListByType(new SearchVO(), ParameterTypeEnum.AIDecisionProcessDisplaySwitch.ToString());
        vm.IsAIDecisionProcessDisplay = sysSettings
            .Select(x => x.Value == "1")
            .FirstOrDefault(true);

        SearchVO contentSearchVO = new();
        contentSearchVO.UploadIdEq = dm.UploadId;
        BomFileContentBL contentBl = BLFactory.GetInstanceBackGround<BomFileContentBL>();
        List<BomFileContentDM> contentList = contentBl.GetListWithQuotationByFilter(contentSearchVO);

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
            TBBomFileQuotationOtherBL otherBl = BLFactory.GetInstanceBackGround<TBBomFileQuotationOtherBL>();
            List<TBBomFileQuotationOtherDM> otherList = otherBl.GetListByFilter(otherSearchVO);

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

        return vm;
    }

    /// <inheritdoc />
    public async Task<QuotationResultServiceResult> ReInternalQuotationAsync(QuotationReInternalQuotationRequestVM request)
    {
        TBBomFileQuotationBL quotationBl = BLFactory.GetInstanceBackGround<TBBomFileQuotationBL>();
        quotationBl.DoUpdateNo(request.BomFileContentId, request.No, false);

        SearchVO contentSearchVO = new();
        contentSearchVO.IdEq = request.BomFileContentId;
        contentSearchVO.IsLimit1 = true;
        BomFileContentBL contentBl = BLFactory.GetInstanceBackGround<BomFileContentBL>();
        BomFileContentDM? content = contentBl.GetListWithQuotationByFilter(contentSearchVO).FirstOrDefault();
        if (content == null)
            return QuotationResultServiceResult.Fail("資料不存在");

        content.IsRecommendedNo = false;

        EsFileTransferUploadBL uploadBl = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();
        EsFileTransferUploadDM? upload = uploadBl.GetOneInfo(content.UploadId);
        string? customerCode = upload?.CustomerCode;

        await _quotationHandler.RunInternalAsync(content, customerCode);

        HandleQuotationBL handleBl = BLFactory.GetInstanceBackGround<HandleQuotationBL>();
        handleBl.DoSaveSingleInternalQuotationResult(content);

        return QuotationResultServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<QuotationResultServiceResult> ReExternalQuotationAsync(QuotationReExternalQuotationRequestVM request)
    {
        SearchVO contentSearchVO = new();
        contentSearchVO.IdEq = request.BomFileContentId;
        contentSearchVO.IsLimit1 = true;
        BomFileContentBL contentBl = BLFactory.GetInstanceBackGround<BomFileContentBL>();
        BomFileContentDM? dmBomFileContent = contentBl.GetListWithQuotationByFilter(contentSearchVO).FirstOrDefault();
        if (dmBomFileContent == null)
            return QuotationResultServiceResult.Fail("資料不存在");

        EsFileTransferUploadBL uploadBl = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();
        EsFileTransferUploadDM? dmEsFileTransferUpload = uploadBl.GetOneInfoByUploadId(dmBomFileContent.UploadId);
        int quotationQty = dmEsFileTransferUpload?.QuotationQty ?? 1;

        await _quotationHandler.AuthorizeExternalAsync();
        await _quotationHandler.RunExternalAsync(dmBomFileContent, quotationQty);

        HandleQuotationBL handleBl = BLFactory.GetInstanceBackGround<HandleQuotationBL>();
        handleBl.DoSaveSingleExternalQuotationResult(dmBomFileContent);

        return QuotationResultServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<QuotationResultServiceResult> CheckInStockPriceAsync(QuotationCheckInStockPriceRequestVM request)
    {
        SearchVO contentSearchVO = new();
        contentSearchVO.IdEq = request.BomFileContentId;
        contentSearchVO.IsLimit1 = true;
        BomFileContentBL contentBl = BLFactory.GetInstanceBackGround<BomFileContentBL>();
        BomFileContentDM? content = contentBl.GetListWithQuotationByFilter(contentSearchVO).FirstOrDefault();
        if (content == null)
            return QuotationResultServiceResult.Fail("資料不存在");

        EsFileTransferUploadBL uploadBl = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();
        EsFileTransferUploadDM? upload = uploadBl.GetOneInfo(content.UploadId);
        int quotationQty = upload?.QuotationQty ?? 1;

        ExternalQueryExecuteHandler.QueryMouserCartPriceReqVO apiReq = new();
        apiReq.PartNumber = content.ManufacturerPartNumber ?? string.Empty;
        apiReq.Quantity = quotationQty;

        Task<QueryActionResultRspVO> mouserTask = _externalQueryExecuteHandler.QueryMouserAction(apiReq);
        Task<QueryActionResultRspVO> dkTask = _externalQueryExecuteHandler.QueryDkAction(apiReq);
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

        HandleQuotationBL handleBl = BLFactory.GetInstanceBackGround<HandleQuotationBL>();
        handleBl.DoSaveSingleInStockPriceResult(
            request.BomFileContentId,
            mouserDm,
            dkDm,
            decisionLogs);

        return QuotationResultServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<object> BuildQuickSearchResultAsync(QuotationQuickSearchRequestVM request)
    {
        BomFileContentDM content = new();
        content.ManufacturerPartNumber = request.ManufacturerPartNumber?.Trim();
        content.ComponentPart = request.ComponentPart?.Trim();
        content.Manufacturer = request.Manufacturer?.Trim();

        HashSet<string> brandComparisonCategorySet = _quotationHandler.GetBrandComparisonCategorySet();
        await _quotationHandler.RunPartSearchAsync(content, brandComparisonCategorySet);

        string? customerCode = string.IsNullOrWhiteSpace(request.CustomerCode) ? null : request.CustomerCode.Trim();
        await _quotationHandler.RunInternalAsync(content, customerCode);

        QuotationItemVM itemVm = _mapper.Map<QuotationItemVM>(content);
        if (content.MatchCategory.HasValue)
            itemVm.MatchCategoryText = ((MatchCategoryEnum)content.MatchCategory.Value).GetDescription();
        if (content.ExternalScenario.HasValue)
            itemVm.ExternalScenarioText = ((ExternalScenarioEnum)content.ExternalScenario.Value).GetDescription();
        itemVm.IsFilterByCustomerApprovedPartText = content.IsFilterByCustomerApprovedPart ? "是" : "否";

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

        object? clusterData = null;
        if (!string.IsNullOrWhiteSpace(content.No))
        {
            SearchVO historySearchVO = new();
            historySearchVO.SanderModuleItemNoEq = content.No;
            historySearchVO.UnitCostLcyGt = 0;
            if (!string.IsNullOrEmpty(content.CustomerApprovedPartCsv))
                historySearchVO.Description2In = content.CustomerApprovedPartCsv.Split(',').ToList();

            SanderModulePurchaseLineBL purchaseBl = BLFactory.GetInstanceBackGround<SanderModulePurchaseLineBL>();
            List<SanderModulePurchaseLineDM> purchases = purchaseBl.GetListByFilter(historySearchVO);

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

        return new { Item = itemVm, DecisionLogs = decisionLogs, ClusterData = clusterData };
    }

    /// <inheritdoc />
    public object GetPriceClusterPageList(
        Guid? bomFileContentId,
        string? no,
        string? customerApprovedPartCsv,
        decimal? lowMinPrice,
        decimal? lowMaxPrice,
        decimal? highMinPrice,
        decimal? highMaxPrice,
        int page,
        int pageSize)
    {
        string? resolvedNo = no;
        string? resolvedCsv = customerApprovedPartCsv;
        object? clusterRanges;

        if (bomFileContentId.HasValue)
        {
            TBBomFileQuotationBL quotationBl = BLFactory.GetInstanceBackGround<TBBomFileQuotationBL>();
            TBBomFileQuotationDM? quotation = quotationBl.GetOneByBomFileContentId(bomFileContentId.Value);
            if (quotation == null)
                return new { ClusterRanges = (object?)null, Records = Array.Empty<object>(), Total = 0 };

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

        SanderModulePurchaseLineBL purchaseBl = BLFactory.GetInstanceBackGround<SanderModulePurchaseLineBL>();
        PageResult<SanderModulePurchaseLineDM> pageResult = purchaseBl.GetPageListPriceCluster(pageEntity, searchVO);

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

        return new { ClusterRanges = clusterRanges, Records = records, Total = pageResult.DataCount };
    }

    /// <summary>
    /// 功能說明：將 ListPageEntity 轉為 PageEntity（分頁、排序欄位對應 SortAttribute）。
    /// </summary>
    /// <typeparam name="T">輸入參數：含 SortAttribute 的 VM 型別。</typeparam>
    /// <param name="request">輸入參數：Page、PageSize、SortField、SortDir。</param>
    /// <returns>輸出參數：PageEntity（CurrentPage、PageDataSize、Sort、Asc）。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：GetPageList；與 ApiBaseController.GetPageEntity 邏輯類似。
    /// 訊息內容及生成條件：無錯誤訊息；未指定 SortField 時用預設 SortAttribute。
    /// </remarks>
    private static PageEntity BuildPageEntity<T>(ListPageEntity request)
        where T : class
    {
        PageEntity result = new();
        result.CurrentPage = request.Page;
        result.PageDataSize = request.PageSize;
        result.Asc = string.IsNullOrWhiteSpace(request.SortDir) || (request.SortDir.ToUpper() != "ASC" && request.SortDir.ToUpper() != "DESC") ? "ASC" : request.SortDir.ToUpper();

        PropertyInfo[] propertyInfoList = typeof(T).GetProperties();
        string defaultSort = string.Empty;
        string defaultAsc = string.Empty;

        foreach (PropertyInfo item in propertyInfoList)
        {
            SortAttribute? attrSort = (SortAttribute?)Attribute.GetCustomAttribute(item, typeof(SortAttribute));
            if (attrSort == null) continue;

            string sort = attrSort.ColumnName ?? item.Name;

            if (string.IsNullOrEmpty(request.SortField) && attrSort.IsDefault)
            {
                defaultSort = sort;
                defaultAsc = attrSort.DefaultSortOrder;
            }

            if (request.SortField == item.Name)
            {
                result.Sort = sort;
                break;
            }
        }

        if (string.IsNullOrEmpty(result.Sort))
        {
            result.Sort = defaultSort;
            result.Asc = defaultAsc;
        }

        return result;
    }

    /// <inheritdoc />
    public List<SelectItemVO> GetCustomerList()
    {
        ReportItemCustomerBL bl = BLFactory.GetInstanceBackGround<ReportItemCustomerBL>();
        return bl.GetListEnabled(new SearchVO())
            .GroupBy(x => x.CustomerCode)
            .Select(x => new SelectItemVO(x.First().CustomerName ?? string.Empty, x.Key ?? string.Empty))
            .ToList();
    }

    /// <inheritdoc />
    public List<object> GetDecisionLogs(Guid bomFileContentId)
    {
        TBBomFileDecisionLogBL bl = BLFactory.GetInstanceBackGround<TBBomFileDecisionLogBL>();
        SearchVO searchVO = new();
        searchVO.BomFileContentIdEq = bomFileContentId;

        return bl.GetListEnabled(searchVO)
            .OrderBy(x => x.Stage)
            .ThenBy(x => x.Step)
            .Select(x => (object)new
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
    }

    /// <inheritdoc />
    public async Task<object> BuildQuickPartMatchResultAsync(QuotationQuickPartMatchRequestVM request)
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

        return new
        {
            No = content.No ?? string.Empty,
            MatchCategoryText = matchCategoryText,
            MatchField = content.MatchField ?? string.Empty,
            DecisionLogs = decisionLogs
        };
    }

    /// <inheritdoc />
    public async Task<object> BuildQuickPricingResultAsync(QuotationQuickPricingRequestVM request)
    {
        object? internalResult = null;
        object? externalResult = null;
        object? inStockResult = null;
        object? clusterMeta = null;
        List<object> decisionLogs = new();

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

        return new
        {
            Internal = internalResult,
            External = externalResult,
            InStock = inStockResult,
            ClusterMeta = clusterMeta,
            DecisionLogs = decisionLogs
        };
    }
}
