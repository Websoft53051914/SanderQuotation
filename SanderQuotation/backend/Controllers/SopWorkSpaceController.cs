using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using Microsoft.AspNetCore.Mvc;
using ViewModel.SopWorkSpace;

namespace backend.Controllers
{
    /// <summary>
    /// SOP 工規工作站 API Controller
    /// </summary>
    [Route("api/SopWorkSpace")]
    public class SopWorkSpaceController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        public SopWorkSpaceController(IConfiguration config) : base(config)
        {
            _config = config;

            MapperConfiguration mapperConfig = new(cfg =>
            {
                cfg.CreateMap<SopOrderDM, SopOrderVM>();
                cfg.CreateMap<SopStationDM, SopStationVM>();
                cfg.CreateMap<SopRuleRowDM, SopRuleRowVM>();
                cfg.CreateMap<SopRemarkDM, SopRemarkVM>();
                cfg.CreateMap<SopStationContentDM, SopStationContentVM>();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private SopWorkSpaceBL? _bl;
        private SopWorkSpaceBL GetBL()
        {
            _bl ??= GetBLInstance<SopWorkSpaceBL>();
            return _bl;
        }

        /// <summary>
        /// 取得工單下拉選單清單（OrderFlowId=4）
        /// </summary>
        [HttpGet("GetOrderList")]
        public IActionResult GetOrderList()
        {
            try
            {
                List<SopOrderDM> dmList = GetBL().GetOrderList();
                List<SopOrderVM> vmList = _mapper.Map<List<SopOrderVM>>(dmList);
                return JsonSuccess(vmList);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 依工單 ID 取得站點下拉選單
        /// </summary>
        /// <param name="orderId">工單 ID</param>
        [HttpGet("GetStationList")]
        public IActionResult GetStationList([FromQuery] string orderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId))
                    return JsonValidFail("orderId 不可為空");

                List<SopStationDM> dmList = GetBL().GetStationList(orderId);
                List<SopStationVM> vmList = _mapper.Map<List<SopStationVM>>(dmList);
                return JsonSuccess(vmList);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 依工單 ID 與工站編號取得工規內容
        /// </summary>
        /// <param name="orderId">工單 ID</param>
        /// <param name="opNo">工站編號</param>
        [HttpGet("GetStationContent")]
        public IActionResult GetStationContent([FromQuery] string orderId, [FromQuery] string opNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(opNo))
                    return JsonValidFail("orderId 與 opNo 不可為空");

                SopStationContentDM? dm = GetBL().GetStationContent(orderId, opNo);
                if (dm == null)
                    return JsonValidFail("查無工規內容");

                SopStationContentVM vm = _mapper.Map<SopStationContentVM>(dm);
                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得工規圖片檔案（代理轉發）
        /// </summary>
        /// <param name="path">圖片絕對路徑</param>
        [HttpGet("GetImage")]
        public IActionResult GetImage([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest();

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif" && ext != ".bmp")
                return BadRequest();

            string fullPath = Path.GetFullPath(path);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            string mimeType = ext switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "image/jpeg"
            };

            byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
            return File(bytes, mimeType);
        }
    }
}
