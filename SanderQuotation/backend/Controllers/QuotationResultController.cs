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
                (int)EsFileTransferUploadProcessStatusEnum.PendingPartSearch => EsFileTransferUploadProcessStatusEnum.PendingPartSearch.GetDescription(),
                (int)EsFileTransferUploadProcessStatusEnum.PartSearchDone => EsFileTransferUploadProcessStatusEnum.PartSearchDone.GetDescription(),
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
                    vm.CustomerName = d.CustomerName;
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
                vm.CustomerName = dm.CustomerName;
                vm.ProdNo = dm.ProdNo;
                vm.QuotationQty = dm.QuotationQty;
                vm.CreatedAtText = dm.CreatedAt?.ToString("yyyy/MM/dd HH:mm") ?? string.Empty;
                vm.UpdatedAtText = dm.UpdatedAt?.ToString("yyyy/MM/dd HH:mm") ?? string.Empty;
                vm.Items = new();

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
                BomFileContentDM? content = GetBlBomFileContent().GetListWithQuotationByFilter(contentSearchVO).FirstOrDefault();
                if (content == null)
                    return JsonValidFail("資料不存在");

                // 取得報價數量
                EsFileTransferUploadDM? upload = GetBlEsFileTransferUpload().GetOneInfo(content.UploadId);
                int quotationQty = upload?.QuotationQty ?? 1;

                await _quotationHandler.AuthorizeExternalAsync();
                await _quotationHandler.RunExternalAsync(content, quotationQty);

                GetBlHandleQuotation().DoSaveSingleExternalQuotationResult(content);

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
        /// 取得指定料項的價格分群資料（歷史採購紀錄 + AI 分群範圍）
        /// </summary>
        [CustomAuthorization(FuncID.QuotationResult_View)]
        [HttpGet("GetPriceClusterData")]
        public ActionResult GetPriceClusterData(Guid bomFileContentId)
        {
            try
            {
                TBBomFileQuotationDM? quotation = GetBlTBBomFileQuotation().GetOneByBomFileContentId(bomFileContentId);
                if (quotation == null)
                    return JsonSuccess(new { ClusterRanges = (object?)null, Records = Array.Empty<object>() });

                List<SanderModulePurchaseLineDM> purchases = new();
                if (!string.IsNullOrWhiteSpace(quotation.No))
                {
                    SearchVO historySearchVO = new();
                    historySearchVO.SanderModuleItemNoEq = quotation.No;
                    historySearchVO.UnitCostLcyGt = 0;
                    if(!string.IsNullOrEmpty(quotation.CustomerApprovedPartCsv))
                    {
                        historySearchVO.Description2In = quotation.CustomerApprovedPartCsv.Split(',').ToList();
                    }
                    purchases = GetBlSanderModulePurchaseLine().GetListByFilter(historySearchVO);
                }

                var clusterRanges = new
                {
                    LowMinPrice = quotation.InternalLowMinPrice,
                    LowMaxPrice = quotation.InternalLowMaxPrice,
                    HighMinPrice = quotation.InternalHighMinPrice,
                    HighMaxPrice = quotation.InternalHighMaxPrice
                };

                var records = purchases.Select(x => new
                {
                    DocumentDate = x.DocumentDate?.ToString("yyyy/MM/dd"),
                    Description2 = x.Description2 ?? string.Empty,
                    BuyFromVendorName = x.BuyFromVendorName ?? string.Empty,
                    UnitCost = x.UnitCost,
                    UnitCostLcy = x.UnitCostLcy,
                    Quantity = x.Quantity,
                    CurrencyCode = x.CurrencyCode ?? string.Empty
                }).ToList();

                return JsonSuccess(new { ClusterRanges = clusterRanges, Records = records });
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
