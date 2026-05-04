using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Web.EX;
using backend.Common;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using Core.Utility.Utility;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class ControlLogController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public ControlLogController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ControlLogDM, ControlLogVM>()
                    .ForMember(dest => dest.LogTime, opt => opt.MapFrom(src => src.LogTime.ToString("yyyy-MM-dd HH:mm:ss")))
                    .ReverseMap();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private LogBL? _logBL;
        private LogBL GetLogBL()
        {
            _logBL ??= GetBLInstance<LogBL>();
            return _logBL;
        }

        /// <summary>
        /// 取得 Log 分頁列表
        /// </summary>
        [HttpGet("GetPageList")]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, string? keyword, DateTime? dateGte, DateTime? dateLte, int? status)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                var pageResult = GetLogBL().GetPageList(pageEntity, keyword ?? string.Empty, dateGte, dateLte, status);

                var list = _mapper.Map<List<ControlLogVM>>(pageResult.Results);
                var dic = ConvertUtility.Enum2Dictionary<Const.Enums.LogAction>();

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].No = ((request.pageIndex - 1) * request.pageSize + i + 1).ToString();
                    list[i].StatusName = list[i].Status == 1 ? "成功" : "失敗";
                    if (dic.ContainsKey(list[i].Action))
                    {
                        list[i].ActionStr = dic[list[i].Action];
                    }
                }

                return JsonSuccess(new
                {
                    Data = list,
                    Total = pageResult.DataCount,
                    Page = pageResult.CurrentPage,
                    PageSize = pageResult.PageDataSize
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得單筆 Exception 內容
        /// </summary>
        [HttpGet("GetException")]
        public IActionResult GetException(Guid id)
        {
            try
            {
                var exception = GetLogBL().GetException(id);
                return JsonSuccess(exception ?? string.Empty);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
