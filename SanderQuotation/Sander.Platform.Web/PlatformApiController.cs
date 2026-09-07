using Business;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;
using CommonClass.CustomAttribute;
using CommonClass.Model;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using static Const.Enums;

namespace Sander.Platform.Web
{
    /// <summary>
    /// 共用 API 基底：JWT UserInfo、GetBLInstance、分頁／JSON、操作日誌。host 與 DbTransfer API 皆繼承此類。
    /// </summary>
    public abstract class PlatformApiController : Controller
    {
        protected readonly IConfiguration _config;

        protected PlatformApiController(IConfiguration configuration)
        {
            _config = configuration;
        }

        public UserInfo UserInfo { get; set; } = new();

        public List<string> ErrorMsgs = new();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            UserInfo ??= new UserInfo();
            UserInfo.UserAccount = User.FindFirst("UserAccount")?.Value;
            UserInfo.UserName = User.FindFirst("UserName")?.Value;
            UserInfo.IP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            if (User.FindFirst("PermissionCodeList") != null)
            {
                UserInfo.PermissionCodeList = JsonConvert.DeserializeObject<List<string>>(User.FindFirst("PermissionCodeList")!.Value)
                    ?? new List<string>();
            }
            base.OnActionExecuting(context);
        }

        public T GetBLInstance<T>() where T : BaseProjectBL
        {
            T bl = BusinessFactory.GetInstance<T>();
            bl.UserInfo = UserInfo;
            bl._Configuration = HttpContext.RequestServices.GetService<IConfiguration>();
            return bl;
        }

        protected string GetMsg(IConfiguration config, string key)
            => config[$"message:zh-tw:{key}"] ?? key;

        private string ResolveLogIp()
        {
            if (!string.IsNullOrEmpty(UserInfo?.IP))
                return UserInfo.IP;
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public void ErrorAlert(string msg)
        {
            ViewBag.ErrorAlertMessage = msg;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public void WarningAlert(string msg)
        {
            ViewBag.WarningAlertMessage = msg;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity(DataSourceRequest request)
        {
            return new PageEntity
            {
                Sort = !string.IsNullOrEmpty(request.SortField) ? request.SortField : string.Empty,
                Asc = string.IsNullOrWhiteSpace(request.SortOrder) || (request.SortOrder.ToUpper() != "ASC" && request.SortOrder.ToUpper() != "DESC")
                    ? "ASC"
                    : request.SortOrder.ToUpper(),
                CurrentPage = request.pageIndex,
                PageDataSize = request.pageSize
            };
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity<T>(DataSourceRequest request)
            where T : class
        {
            var result = new PageEntity
            {
                CurrentPage = request.pageIndex,
                PageDataSize = request.pageSize,
                Asc = string.IsNullOrWhiteSpace(request.SortOrder) || (request.SortOrder.ToUpper() != "ASC" && request.SortOrder.ToUpper() != "DESC")
                    ? "ASC"
                    : request.SortOrder.ToUpper()
            };

            var propertyInfoList = typeof(T).GetProperties();
            var defaultSort = string.Empty;
            var defaultAsc = string.Empty;
            foreach (var item in propertyInfoList)
            {
                var attrSortColumn = (SortAttribute?)Attribute.GetCustomAttribute(item, typeof(SortAttribute));
                if (attrSortColumn == null)
                    continue;

                var sort = attrSortColumn.ColumnName ?? item.Name;

                if (string.IsNullOrEmpty(request.SortField) && attrSortColumn.IsDefault)
                {
                    defaultSort = sort;
                    defaultAsc = attrSortColumn.DefaultSortOrder;
                }

                if (request.SortField == item.Name)
                {
                    result.Sort = sort;
                    break;
                }
            }

            if (string.IsNullOrEmpty(result.Sort))
            {
                result.Sort = defaultSort;
                result.Asc = defaultAsc;
            }

            return result;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity(ListPageEntity request)
        {
            return new PageEntity
            {
                Sort = !string.IsNullOrEmpty(request.SortField) ? request.SortField : string.Empty,
                Asc = string.IsNullOrWhiteSpace(request.SortDir) || (request.SortDir.ToUpper() != "ASC" && request.SortDir.ToUpper() != "DESC")
                    ? "ASC"
                    : request.SortDir.ToUpper(),
                CurrentPage = request.Page,
                PageDataSize = request.PageSize
            };
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity<T>(ListPageEntity request)
            where T : class
        {
            var result = new PageEntity
            {
                CurrentPage = request.Page,
                PageDataSize = request.PageSize,
                Asc = string.IsNullOrWhiteSpace(request.SortDir) || (request.SortDir.ToUpper() != "ASC" && request.SortDir.ToUpper() != "DESC")
                    ? "ASC"
                    : request.SortDir.ToUpper()
            };

            var propertyInfoList = typeof(T).GetProperties();
            var defaultSort = string.Empty;
            var defaultAsc = string.Empty;

            foreach (var item in propertyInfoList)
            {
                var attrSort = (SortAttribute?)Attribute.GetCustomAttribute(item, typeof(SortAttribute));
                if (attrSort == null)
                    continue;

                var sort = attrSort.ColumnName ?? item.Name;

                if (string.IsNullOrEmpty(request.SortField) && attrSort.IsDefault)
                {
                    defaultSort = sort;
                    defaultAsc = attrSort.DefaultSortOrder;
                }

                if (request.SortField == item.Name)
                {
                    result.Sort = sort;
                    break;
                }
            }

            if (string.IsNullOrEmpty(result.Sort))
            {
                result.Sort = defaultSort;
                result.Asc = defaultAsc;
            }

            return result;
        }

        protected JsonResult JsonPage(DataSourceResult data)
        {
            return Json(data);
        }

        protected JsonResult JsonOK<T>(T data)
        {
            return Json(data);
        }

        protected JsonResult JsonSuccess<T>(T data)
        {
            return Json(new { Success = true, Data = data });
        }

        protected JsonResult JsonOK()
        {
            return Json(new { Success = true });
        }

        protected JsonResult JsonOK(string message)
        {
            return Json(new { Success = true, Data = message });
        }

        protected JsonResult JsonValidFail(string errorData)
        {
            return Json(new { Success = false, Message = errorData });
        }

        protected JsonResult JsonValidFail<T1>(T1 errorData)
        {
            return Json(new { Success = false, Errors = errorData });
        }

        protected JsonResult JsonValiFail<T1, T2>(T1 errorData, T2 data)
        {
            return Json(new { Success = false, Errors = errorData, Data = data });
        }

        protected JsonResult JsonValiFail(string message)
        {
            return Json(new { Success = false, Data = message });
        }

        protected JsonResult JsonValiFailFromModelState(ModelStateDictionary modelstate)
        {
            string errorMsg = "";
            foreach (var item in modelstate.Where(w => w.Value != null && w.Value.Errors.Count > 0).Select(w => w.Value!.Errors.Select(s => s.ErrorMessage)))
            {
                errorMsg += string.Join(",", item) + ",";
            }

            return Json(new { Success = false, Message = errorMsg });
        }

        protected string GetDomainName()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        }

        protected Guid LogError(string exception)
        {
            var blLog = GetBLInstance<LogBL>();
            var logDM = new ControlLogDM
            {
                IP = ResolveLogIp(),
                Status = (int)LogStatusEnum.Failed,
                ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
                ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
                Exception = exception
            };
            return blLog.InsertLog(logDM);
        }

        protected void LogError(Exception exception)
        {
            LogError(exception.ToString());
        }

        protected void LogSuccess(Guid id, LogAction logAction, string? description = null)
        {
            var blLog = GetBLInstance<LogBL>();
            var logDM = new ControlLogDM
            {
                IP = ResolveLogIp(),
                Status = (int)LogStatusEnum.Success,
                ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
                ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
                Exception = description,
                DataId = id,
                Action = (int)logAction
            };
            blLog.InsertLog(logDM);
        }

        protected void LogSuccess(string? description = null)
        {
            var blLog = GetBLInstance<LogBL>();
            var logDM = new ControlLogDM
            {
                IP = ResolveLogIp(),
                Status = (int)LogStatusEnum.Success,
                ControllerName = ControllerContext.ActionDescriptor?.ControllerName ?? string.Empty,
                ActionName = ControllerContext.ActionDescriptor?.ActionName ?? string.Empty,
                Exception = description
            };
            blLog.InsertLog(logDM);
        }
    }
}
