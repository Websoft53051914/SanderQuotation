using AutoMapper;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/SysSetting")]
    public class SysSettingController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public SysSettingController(IConfiguration config) : base(config)
        {
            _config = config;

            //var mapperConfig = new MapperConfiguration(cfg =>
            //{
            //    cfg.CreateMap<ESDbTransferVM, ESDbTransferDM>().ReverseMap();
            //});
            //_mapper = mapperConfig.CreateMapper();
        }

        private TBSysSettingBL? _sysSettingBL;
        private TBSysSettingBL GetSysSettingBL()
        {
            _sysSettingBL ??= GetBLInstance<TBSysSettingBL>();
            return _sysSettingBL;
        }

        [HttpGet("GetData")]
        [CustomAuthorization(FuncID.SysSetting_View)]
        public IActionResult GetData()
        {
            try
            {
                var list = GetSysSettingBL().GetListEnabled(new SearchVO()
                {

                });
                list = list.Where(x => x.Type != ParameterTypeEnum.Internal.ToString()).ToList();
                SysSettingVM vM = new();
                vM.IsAIDecisionProcessDisplay = list.Where(x => x.Type == ParameterTypeEnum.AIDecisionProcessDisplaySwitch.ToString()).Select(x => x.Value == "1").FirstOrDefault();
                vM.PreferredVendorList = list.Where(x => x.Type == ParameterTypeEnum.PreferredVendorList.ToString()).Select(x => new SysSettingVM.ListItemVM(){ Id = x.Id, Value = x.Value }).ToList();
                vM.BrandComparisonCategoryList = list.Where(x => x.Type == ParameterTypeEnum.BrandComparisonCategoryList.ToString()).Select(x => new SysSettingVM.ListItemVM(){ Id = x.Id, Value = x.Value }).ToList();
                return JsonSuccess(vM);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Delete")]
        [CustomAuthorization(FuncID.SysSetting_View)]
        public IActionResult Delete(Guid id)
        {
            try
            {
                GetSysSettingBL().Delete(id);
                LogSuccess(id, LogAction.Delete);
                return JsonSuccess("刪除成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Create")]
        [CustomAuthorization(FuncID.SysSetting_View)]
        public IActionResult Create([FromBody] SysSettingVM.ListItemVM value)
        {
            try
            {
                GetSysSettingBL().CheckExist(new TBSysSettingDM()
                {
                    Param = value.Value,
                    Value = value.Value,
                    Type = value.Type
                });
                if (GetSysSettingBL().GetMessage().IsError())
                {
                    return JsonValidFail(GetSysSettingBL().GetMessage().GetErrMsg());
                }
                GetSysSettingBL().Insert(new TBSysSettingDM()
                {
                    Param = value.Value,
                    Value = value.Value,
                    Type = value.Type
                });
                LogSuccess();
                return JsonSuccess("新增成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Edit")]
        [CustomAuthorization(FuncID.SysSetting_View)]
        public IActionResult Edit([FromBody] SysSettingVM.ListItemVM value)
        {
            try
            {
                GetSysSettingBL().CheckExist(new TBSysSettingDM()
                {
                    Param = value.Value,
                    Value = value.Value,
                    Type = value.Type,
                    Id = value.Id,
                });
                if (GetSysSettingBL().GetMessage().IsError())
                {
                    return JsonValidFail(GetSysSettingBL().GetMessage().GetErrMsg());
                }
                GetSysSettingBL().Edit(new TBSysSettingDM()
                {   
                    Id = value.Id,
                    Param = value.Value,
                    Value = value.Value,
                    Type = value.Type
                });
                LogSuccess();
                return JsonSuccess("編輯成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("SaveOther")]
        [CustomAuthorization(FuncID.SysSetting_View)]
        public IActionResult SaveOther([FromBody] SysSettingVM value)
        {
            try
            {
                GetSysSettingBL().DoSave(new List<TBSysSettingDM>()
                {
                    new TBSysSettingDM()
                    {
                        Param = value.IsAIDecisionProcessDisplay ? "1" : "0",
                        Value = value.IsAIDecisionProcessDisplay ? "1" : "0",
                        Type = ParameterTypeEnum.AIDecisionProcessDisplaySwitch.ToString(),
                    }
                });
                LogSuccess();
                return JsonSuccess("編輯成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
