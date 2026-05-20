using AutoMapper;
using backend.Common.Attribute;
using backend.Models;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
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

        /// <summary>
        /// constructor
        /// </summary>
        public QuotationResultController(IConfiguration configuration, QuotationHandler quotationHandler) : base(configuration)
        {
            _quotationHandler = quotationHandler;
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
            });
            _mapper = cfg.CreateMapper();
        }

        private EsFileTransferUploadBL? _blEsFileTransferUpload = null;
        /// <summary>
        /// EsFileTransferUploadBL
        /// </summary>
        protected EsFileTransferUploadBL GetBlEsFileTransferUpload()
        {
            _blEsFileTransferUpload ??= GetBLInstance<EsFileTransferUploadBL>();
            return _blEsFileTransferUpload;
        }

        private BomFileContentBL? _blBomFileContent = null;
        /// <summary>
        /// BomFileContentBL
        /// </summary>
        protected BomFileContentBL GetBlBomFileContent()
        {
            _blBomFileContent ??= GetBLInstance<BomFileContentBL>();
            return _blBomFileContent;
        }

        private HandleQuotationBL? _blHandleQuotation = null;
        /// <summary>
        /// HandleQuotationBL
        /// </summary>
        protected HandleQuotationBL GetBlHandleQuotation()
        {
            _blHandleQuotation ??= GetBLInstance<HandleQuotationBL>();
            return _blHandleQuotation;
        }

        private TBBomFileQuotationBL? _blTBBomFileQuotation = null;
        /// <summary>
        /// TBBomFileQuotationBL
        /// </summary>
        protected TBBomFileQuotationBL GetBlTBBomFileQuotation()
        {
            _blTBBomFileQuotation ??= GetBLInstance<TBBomFileQuotationBL>();
            return _blTBBomFileQuotation;
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
                foreach (BomFileContentDM contentDm in GetBlBomFileContent().GetListWithQuotationByFilter(contentSearchVO))
                {
                    vm.Items.Add(_mapper.Map<QuotationItemVM>(contentDm));
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

        #endregion
    }
}
