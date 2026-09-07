using AutoMapper;
using backend.Common;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

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
        [CustomAuthorization(FuncID.Log_View)]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, string? keyword, DateTime? dateGte, DateTime? dateLte, int? status)
        {
            try
            {   
                if(dateLte.HasValue)
                {
                    dateLte = dateLte.Value.Date.AddDays(1).AddTicks(-1);
                }
                var pageEntity = base.GetPageEntity(request);
                var pageResult = GetLogBL().GetPageList(pageEntity, keyword ?? string.Empty, dateGte, dateLte, status);

                var list = _mapper.Map<List<ControlLogVM>>(pageResult.Results);
                var dic = ConvertUtility.Enum2Dictionary<Const.Enums.LogAction>();

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].No = ((pageEntity.CurrentPage - 1) * pageEntity.PageDataSize + i + 1).ToString();
                    list[i].StatusName = EnumUtility.GetDescriptionByInt<LogStatusEnum>(list[i].Status??0);
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
        [CustomAuthorization(FuncID.Log_View)]
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
