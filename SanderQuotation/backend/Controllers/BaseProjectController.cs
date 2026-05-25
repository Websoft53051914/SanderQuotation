using backend.Common;
using backend.Common.ConfigurationHelper;
using backend.Models;
using Business;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Helper.Message;
using Core.Utility.Web.Base;
using MES.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using static Const.Enums;

namespace backend.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Authorize]
    public partial class BaseProjectController : ApiBaseController
    {
        /// <summary>
        /// 
        /// </summary>
        protected readonly IConfiguration _config;
        /// <summary>
        /// 
        /// </summary>
        protected readonly ConfigurationHelper _configHelper;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public BaseProjectController(IConfiguration configuration)
        {
            _config = configuration;
            _configHelper = new ConfigurationHelper(configuration);
        }

        public CommonClass.Model.UserInfo UserInfo { get; set; } = null;



        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (UserInfo == null)
                UserInfo = new CommonClass.Model.UserInfo();

            UserInfo.UserAccount = User.FindFirst("UserAccount")?.Value;
            UserInfo.UserName = User.FindFirst("UserName")?.Value;
            UserInfo.IP = Common.Method.GetClientIPAddress();
            if (User.FindFirst("PermissionCodeList") != null)
            {
                UserInfo.PermissionCodeList = JsonConvert.DeserializeObject<List<string>>(User.FindFirst("PermissionCodeList").Value) ?? new List<string>();
            }
            //UserInfo.RoleList = User.FindFirst("RoleList")?.Value;

            base.OnActionExecuting(context);
        }




        public T GetBLInstance<T>() where T : BaseProjectBL
        {
            T bl = BusinessFactory.GetInstance<T>();
            bl.UserInfo = UserInfo;
            bl._Configuration = HttpContext.RequestServices.GetService<IConfiguration>();
            return bl;
        }



        #region -- Instance --

        private MessageHelper? _msgHelper = null;
        /// <summary>
        /// 錯誤訊息資訊
        /// </summary>
        /// <returns></returns>
        protected MessageHelper GetMessage()
        {
            _msgHelper ??= new MessageHelper();
            return _msgHelper;
        }

        /// <summary>
        /// 依目前語系取得 message.json 中的訊息
        /// </summary>
        protected string GetMsg(IConfiguration config, string key)
            => config[$"message:zh-tw:{key}"] ?? key;

        //private UserInfoFromTokenResultDTO? _userInfoExt;
        ///// <summary>
        ///// 由 DB function 取得的延伸使用者資訊（EmpID / DeptID / DeptName）。
        ///// 首次存取時才呼叫 DB，同一 Request 內快取。
        ///// </summary>
        //protected UserInfoFromTokenResultDTO UserInfoExt
        //    => _userInfoExt ??= GetBLInstance<SysUserBL>().GetUserInfoFromToken();


        #endregion  -- Instance --

        /// <summary>
        /// 轉址至指定位置並顯示訊息
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <param name="message"></param>
        /// <param name="alertType"></param>
        /// <returns></returns>
        protected IActionResult RedirectToAlertMsg(string actionName, string controllerName, string message, string alertType = "success")
        {
            var paras = new AlertMsgRedirection()
            {
                ActionName = actionName,
                ControllerName = controllerName,
                Msgs = new List<string>() { message },
                AlertType = alertType,
                Paras = [],
            };

            if (HttpContext.Request != null && HttpContext.Request.Query.ContainsKey("className"))
            {
                paras.ClassName = HttpContext.Request.Query["className"].FirstOrDefault() ?? string.Empty;
            }

            if (ViewData.ContainsKey("FuncId"))
            {
                paras.Paras["funcId"] = ViewData["FuncId"]?.ToString() ?? string.Empty;
            }

            // 以 Paras 傳遞，AlertMsg/Redirection 無法接收
            paras.ParasJson = Newtonsoft.Json.JsonConvert.SerializeObject(paras.Paras);

            return RedirectToAction("Redirection", "AlertMsg", paras);
        }


        ///// <summary>
        ///// 紀錄失敗訊息於資料庫
        ///// </summary>
        ///// <param name="exception"></param>
        ///// <returns>ControlLog.Id</returns>
        //protected void LogError(string oldDataJson, string newDataJson, string exception)
        //{
        //    var blLog = BLFactory.GetInstance<LogBL>();
        //    blLog.InsertLog(
        //        UserInfo,
        //        false,
        //        ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
        //        ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
        //        oldDataJson,
        //        newDataJson,
        //        exception);
        //}

        /// <summary>
        /// 紀錄失敗訊息於資料庫
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>ControlLog.Id</returns>
        protected Guid LogError(string exception)
        {
            var blLog = GetBLInstance<LogBL>();

            var logDM = new ControlLogDM()
            {
                IP = LoginSession.Current.IP ?? Method.GetClientIPAddress(),
                Status = ((int)LogStatusEnum.Failed),
                ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
                ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
                Exception = exception
            };

            return blLog.InsertLog(logDM);
        }

        /// <summary>
        /// 紀錄失敗訊息於資料庫
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>ControlLog.Id</returns>
        protected void LogError(Exception exception)
        {
            LogError(exception.ToString());
        }

        ///// <summary>
        ///// 紀錄成功訊息於資料庫
        ///// </summary>
        ///// <param name="description"></param>
        //protected void LogSuccess(string oldDataJson, string newDataJson, string description)
        //{
        //    var blLog = BLFactory.GetInstance<LogBL>();
        //    blLog.InsertLog(
        //        UserInfo,
        //        true,
        //        ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
        //        ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
        //        oldDataJson,
        //        newDataJson,
        //        description);
        //}

        /// <summary>
        /// 紀錄成功訊息於資料庫
        /// </summary>
        /// <param name="description"></param>
        protected void LogSuccess(string description = null)
        {
            var blLog = GetBLInstance<LogBL>();

            var logDM = new ControlLogDM()
            {
                IP = LoginSession.Current.IP ?? Method.GetClientIPAddress(),
                Status = ((int)LogStatusEnum.Success),
                ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
                ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
                Exception = description,
            };

            blLog.InsertLog(logDM);
        }

        /// <summary>
        /// 紀錄成功訊息於資料庫
        /// </summary>
        /// <param name="description"></param>
        protected void LogSuccess(Guid id, LogAction logAction, string description = null)
        {
            var blLog = GetBLInstance<LogBL>();

            var logDM = new ControlLogDM()
            {
                IP = LoginSession.Current.IP ?? Method.GetClientIPAddress(),
                Status = ((int)LogStatusEnum.Success),
                ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
                ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
                Exception = description,
                DataId = id,
                Action = (int)logAction,
            };

            blLog.InsertLog(logDM);
        }

        private SelectListHandler? _selectListHandler = null;
        /// <summary>
        /// SelectListHandler
        /// </summary>
        /// <returns></returns>
        private SelectListHandler GetSelectListHandler()
        {
            _selectListHandler ??= new SelectListHandler(_config);
            return _selectListHandler;
        }
    }

    public partial class BaseProjectController
    {
        private SanderModuleItemBL? _blSanderModuleItem = null;

        /// <summary>
        /// 取得 SanderModuleItemBL 實例
        /// </summary>
        protected SanderModuleItemBL GetBlSanderModuleItem()
        {
            _blSanderModuleItem ??= GetBLInstance<SanderModuleItemBL>();

            return _blSanderModuleItem;
        }

        private SanderModuleItemVariantBL? _blSanderModuleItemVariant = null;

        /// <summary>
        /// 取得 SanderModuleItemVariantBL 實例
        /// </summary>
        protected SanderModuleItemVariantBL GetBlSanderModuleItemVariant()
        {
            _blSanderModuleItemVariant ??= GetBLInstance<SanderModuleItemVariantBL>();

            return _blSanderModuleItemVariant;
        }

        private ReportItemCustomerBL? _blReportItemCustomer = null;

        /// <summary>
        /// 取得 ReportItemCustomerBL 實例
        /// </summary>
        protected ReportItemCustomerBL GetBlReportItemCustomer()
        {
            _blReportItemCustomer ??= GetBLInstance<ReportItemCustomerBL>();

            return _blReportItemCustomer;
        }

        private SanderModulePurchaseLineBL? _blSanderModulePurchaseLine = null;

        /// <summary>
        /// 取得 SanderModulePurchaseLineBL 實例
        /// </summary>
        protected SanderModulePurchaseLineBL GetBlSanderModulePurchaseLine()
        {
            _blSanderModulePurchaseLine ??= GetBLInstance<SanderModulePurchaseLineBL>();

            return _blSanderModulePurchaseLine;
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

        private TBBomFileDecisionLogBL? _blTBBomFileDecisionLog = null;
        /// <summary>
        /// TBBomFileDecisionLogBL
        /// </summary>
        protected TBBomFileDecisionLogBL GetBlTBBomFileDecisionLog()
        {
            _blTBBomFileDecisionLog ??= GetBLInstance<TBBomFileDecisionLogBL>();
            return _blTBBomFileDecisionLog;
        }

        private TBBomFileQuotationOtherBL? _blTBBomFileQuotationOther = null;
        /// <summary>
        /// TBBomFileQuotationOtherBL
        /// </summary>
        protected TBBomFileQuotationOtherBL GetBlTBBomFileQuotationOther()
        {
            _blTBBomFileQuotationOther ??= GetBLInstance<TBBomFileQuotationOtherBL>();
            return _blTBBomFileQuotationOther;
        }

        private TBSysSettingBL? _blTBSysSetting = null;
        /// <summary>
        /// TBSysSettingBL
        /// </summary>
        protected TBSysSettingBL GetBlTBSysSetting()
        {
            _blTBSysSetting ??= GetBLInstance<TBSysSettingBL>();
            return _blTBSysSetting;
        }
    }
}
