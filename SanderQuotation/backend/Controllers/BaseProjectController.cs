using backend.Common;
using backend.Common.ConfigurationHelper;
using backend.Models;
using Business.BusinessLogic;
using Core.Utility.Helper.Message;
using MES.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sander.Platform.Web;

namespace backend.Controllers
{
    /// <summary>
    /// Host API 基底：共用 <see cref="PlatformApiController"/>，另加組態、轉址與報價 BL 捷徑。
    /// </summary>
    [Authorize]
    public partial class BaseProjectController : PlatformApiController
    {
        protected readonly ConfigurationHelper _configHelper;

        public BaseProjectController(IConfiguration configuration) : base(configuration)
        {
            _configHelper = new ConfigurationHelper(configuration);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            UserInfo.IP = Method.GetClientIPAddress();
        }

        private MessageHelper? _msgHelper = null;
        protected MessageHelper GetMessage()
        {
            _msgHelper ??= new MessageHelper();
            return _msgHelper;
        }

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

            paras.ParasJson = Newtonsoft.Json.JsonConvert.SerializeObject(paras.Paras);

            return RedirectToAction("Redirection", "AlertMsg", paras);
        }

        private SelectListHandler? _selectListHandler = null;
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
