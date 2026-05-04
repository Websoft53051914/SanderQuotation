using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Web.EX;
using backend.Common;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class SysFuncClassController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public SysFuncClassController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SysFuncClassVM, SysFuncClassDM>().ReverseMap();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private SysFuncClassBL? _sysFuncClassBL;
        private SysFuncClassBL GetSysFuncClassBL()
        {
            _sysFuncClassBL ??= GetBLInstance<SysFuncClassBL>();
            return _sysFuncClassBL;
        }

        /// <summary>
        /// 取得分頁列表
        /// </summary>
        [HttpGet("GetPageList")]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, SysFuncClassVM vm)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                var dm = _mapper.Map<SysFuncClassDM>(vm);
                var pageResult = GetSysFuncClassBL().GetPageList(pageEntity, dm);

                var list = _mapper.Map<List<SysFuncClassVM>>(pageResult.Results);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.No = (request.pageIndex - 1) * request.pageSize + i + 1;
                    item.StatusName = item.Status == 1 ? "啟用" : "停用";
                }

                return JsonSuccess(new
                {
                    Data = list,
                    Total = pageResult.DataCount
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        [HttpPost("Get")]
        public IActionResult Get(Guid id)
        {
            try
            {
                var dm = GetSysFuncClassBL().GetInfo(id);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                var vm = _mapper.Map<SysFuncClassVM>(dm);
                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 新增
        /// </summary>
        [HttpPost("Create")]
        public IActionResult Create(SysFuncClassVM vm)
        {
            try
            {
                // 驗證類別名稱是否已存在
                var existingClass = GetSysFuncClassBL().CheckExist(vm.ClassName);
                if (existingClass != null)
                {
                    return JsonValidFail("類別名稱已存在");
                }

                var dm = _mapper.Map<SysFuncClassDM>(vm);
                var guid = GetSysFuncClassBL().Create(dm);
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

        /// <summary>
        /// 編輯
        /// </summary>
        [HttpPost("Edit")]
        public IActionResult Edit(SysFuncClassVM vm)
        {
            try
            {
                // 驗證類別名稱是否已存在（排除自己）
                var existingClass = GetSysFuncClassBL().CheckExist(vm.ClassName);
                if (existingClass != null && existingClass.Id != vm.Id)
                {
                    return JsonValidFail("類別名稱已存在");
                }

                var dm = _mapper.Map<SysFuncClassDM>(vm);
                GetSysFuncClassBL().Edit(dm);
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

        /// <summary>
        /// 刪除（批次）
        /// </summary>
        [HttpPost("Delete")]
        public IActionResult Delete(List<Guid> list)
        {
            try
            {
                if (list == null || list.Count == 0)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                foreach (var id in list)
                {
                    GetSysFuncClassBL().Delete(id);
                    LogSuccess(id, LogAction.Delete);
                }

                return JsonSuccess("刪除成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 啟用/停用
        /// </summary>
        [HttpPost("Enable")]
        public IActionResult Enable(Guid id, int enable)
        {
            try
            {
                GetSysFuncClassBL().Enable(id, enable);
                LogSuccess(id, LogAction.Edit);
                return JsonSuccess(enable == 1 ? "啟用成功" : "停用成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得所有功能類別（下拉選單用）
        /// </summary>
        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            try
            {
                var list = GetSysFuncClassBL().GetAll();
                return JsonSuccess(list.Select(x => new
                {
                    CodeID = x.Id.ToString(),
                    CodeName = x.ClassName
                }));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
