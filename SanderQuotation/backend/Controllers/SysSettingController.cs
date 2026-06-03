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

        /// <summary>
        /// 功能說明：建立系統參數設定 Controller。
        /// </summary>
        /// <param name="config">輸入參數：應用程式組態。</param>
        /// <remarks>
        /// 參考功能名稱與用途：BaseProjectController — 基底授權與 GetMsg。
        /// 訊息內容及生成條件：建構子本身不產生 API 回應。
        /// </remarks>
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
        /// <summary>
        /// 功能說明：取得 TBSysSettingBL 單例（延遲建立）。
        /// </summary>
        /// <returns>輸出參數：TBSysSettingBL 實例。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetBLInstance — 建立並注入 UserInfo 的 BL。
        /// 訊息內容及生成條件：無 HTTP 回應。
        /// </remarks>
        private TBSysSettingBL GetSysSettingBL()
        {
            _sysSettingBL ??= GetBLInstance<TBSysSettingBL>();
            return _sysSettingBL;
        }

        /// <summary>
        /// 功能說明：取得系統參數設定（AI 決策顯示開關、偏好廠商、品牌比對類別等）。
        /// </summary>
        /// <returns>輸出參數：JsonSuccess(SysSettingVM)；失敗時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetSysSettingBL().GetListEnabled — 查詢啟用中的設定列；過濾 Internal 類型。
        /// 訊息內容及生成條件：成功 → JsonSuccess(vM)；例外 → LogError 後 JsonValidFail(GetMsg System_Error)。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：刪除單筆系統參數清單項目。
        /// </summary>
        /// <param name="id">輸入參數：設定主鍵 Guid。</param>
        /// <returns>輸出參數：JsonSuccess「刪除成功」；失敗時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：GetSysSettingBL().Delete — 刪除資料；LogSuccess — 寫入操作日誌。
        /// 訊息內容及生成條件：成功 → JsonSuccess「刪除成功」；例外 → JsonValidFail(System_Error)。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：新增系統參數清單項目（偏好廠商或品牌比對類別）。
        /// </summary>
        /// <param name="value">輸入參數：Value、Type 等清單項目欄位。</param>
        /// <returns>輸出參數：JsonSuccess「新增成功」；驗證失敗或例外時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：CheckExist — 檢查重複；Insert — 寫入；GetMessage().GetErrMsg — BL 錯誤訊息。
        /// 訊息內容及生成條件：CheckExist 錯誤 → JsonValidFail(BL 訊息)；成功 →「新增成功」；例外 → System_Error。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：編輯系統參數清單項目。
        /// </summary>
        /// <param name="value">輸入參數：Id、Value、Type 等。</param>
        /// <returns>輸出參數：JsonSuccess「編輯成功」；驗證失敗或例外時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：CheckExist、Edit — 驗證並更新 TBSysSetting。
        /// 訊息內容及生成條件：CheckExist 錯誤 → JsonValidFail(BL 訊息)；成功 →「編輯成功」；例外 → System_Error。
        /// </remarks>
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

        /// <summary>
        /// 功能說明：儲存其他系統參數（目前為 AI 決策過程顯示開關）。
        /// </summary>
        /// <param name="value">輸入參數：IsAIDecisionProcessDisplay 等。</param>
        /// <returns>輸出參數：JsonSuccess「編輯成功」；例外時 JsonValidFail。</returns>
        /// <remarks>
        /// 參考功能名稱與用途：DoSave — 批次儲存 TBSysSettingDM（AIDecisionProcessDisplaySwitch 類型，值 0/1）。
        /// 訊息內容及生成條件：成功 →「編輯成功」；例外 → System_Error。
        /// </remarks>
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
