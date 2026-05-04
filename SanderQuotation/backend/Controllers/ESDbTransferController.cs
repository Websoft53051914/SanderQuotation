using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using backend.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlX.XDevAPI.Common;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/ESDbTransfer")]
    public class ESDbTransferController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public ESDbTransferController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ESDbTransferVM, ESDbTransferDM>().ReverseMap();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private ESDbTransferBL? _ESDbTransferBL;
        private ESDbTransferBL GetESDbTransferBL()
        {
            _ESDbTransferBL ??= GetBLInstance<ESDbTransferBL>();
            return _ESDbTransferBL;
        }

        [HttpGet("GetPageList")]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, ESDbTransferVM vm)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                IMapper mapper = CommonUtility.CreateMapper<ESDbTransferVM, ESDbTransferDM>();
                ESDbTransferDM dm = mapper.Map<ESDbTransferDM>(vm);
                var pageResult = GetESDbTransferBL().GetPageList(pageEntity, dm);

                var list = _mapper.Map<List<ESDbTransferVM>>(pageResult.Results);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.No = (request.pageIndex - 1) * request.pageSize + i + 1;
                    if (string.IsNullOrEmpty(list[i].Description))
                        list[i].Description = "";
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

        [HttpPost("Get")]
        public IActionResult Get(Guid id)
        {
            try
            {
                var dm = GetESDbTransferBL().Get(id);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                var vm = _mapper.Map<ESDbTransferVM>(dm);
                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Create")]
        public IActionResult Create(ESDbTransferVM vm)
        {
            try
            {
                var dm = _mapper.Map<ESDbTransferDM>(vm);
                var guid = GetESDbTransferBL().Create(dm);
                LogSuccess(guid, LogAction.Create);
                return JsonSuccess("新增成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Edit")]
        public IActionResult Edit(ESDbTransferVM vm)
        {
            try
            {
                var dm = _mapper.Map<ESDbTransferDM>(vm);
                GetESDbTransferBL().Edit(dm);
                LogSuccess(vm.Id, LogAction.Edit);
                return JsonSuccess("編輯成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Delete")]
        public IActionResult Delete(List<Guid> list)
        {
            try
            {
                if (list == null || list.Count == 0)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                GetESDbTransferBL().Delete(list);
                foreach (var item in list)
                {
                    LogSuccess(item, LogAction.Delete);
                }
                return JsonSuccess("刪除成功");

            }
            catch (Exception ex)
            {
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
