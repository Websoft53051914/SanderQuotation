using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using static Const.Enums;

namespace Sander.Platform.Web
{
    /// <summary>
    /// 依 JWT claims 的 PermissionCodeList 檢查 FuncID。backend 與 DbTransfer API 共用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CustomAuthorizationAttribute : ActionFilterAttribute
    {
        public IEnumerable<int> functions { get; set; }

        public CustomAuthorizationAttribute(params FuncID[] functions)
        {
            this.functions = functions.Select(x => (int)x);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (functions == null || !functions.Any())
                return;

            string userAccount = context.HttpContext.User.FindFirst("UserAccount")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(userAccount))
            {
                Deny(context);
                return;
            }

            List<string> permissionCodeList = new();
            if (context.HttpContext.User.FindFirst("PermissionCodeList") != null)
            {
                permissionCodeList = JsonConvert.DeserializeObject<List<string>>(context.HttpContext.User.FindFirst("PermissionCodeList")!.Value)
                    ?? new List<string>();
            }

            bool hasPermission = functions.Any(f => permissionCodeList.Contains(f.ToString()));
            if (!hasPermission)
                Deny(context);
        }

        private static void Deny(ActionExecutingContext context)
        {
            if (IsAjaxRequest(context.HttpContext.Request))
            {
                context.HttpContext.Response.StatusCode = 403;
                context.Result = new JsonResult(new { Success = false, Message = "權限不足" });
            }
            else
            {
                context.Result = new ObjectResult(new { Success = false, Message = "權限不足" })
                {
                    StatusCode = 403
                };
            }
        }

        public static bool IsAjaxRequest(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Headers != null)
                return request.Headers["X-Requested-With"] == "XMLHttpRequest";
            return false;
        }
    }
}
