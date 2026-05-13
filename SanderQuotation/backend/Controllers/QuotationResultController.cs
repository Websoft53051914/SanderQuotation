using AutoMapper;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
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

        /// <summary>
        /// constructor
        /// </summary>
        public QuotationResultController(IConfiguration configuration) : base(configuration)
        {
            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<BomFileContentQuotationDTO, QuotationItemVM>()
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
                foreach (BomFileContentQuotationDTO dto in GetBlBomFileContent().GetListWithQuotationByUploadId(dm.UploadId))
                {
                    vm.Items.Add(_mapper.Map<QuotationItemVM>(dto));
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
        /// 重新查價：接收勾選料項及採購型號，執行查價作業
        /// </summary>
        [CustomAuthorization(FuncID.QuotationResult_Edit)]
        [HttpPost("ReQuotation")]
        public ActionResult ReQuotation([FromBody] QuotationReQuotationRequestVM request)
        {
            try
            {
                // TODO: 實作查價業務邏輯
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
