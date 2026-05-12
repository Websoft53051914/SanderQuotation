using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using static Const.Enums;

namespace backend.Common.Attribute
{
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
            // 沒有設定任何需求權限時，直接放行
            if (functions == null || !functions.Any())
                return;

            // 取得登入帳號
            string userAccount = context.HttpContext.User.FindFirst("UserAccount")?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(userAccount))
            {
                Deny(context);
                return;
            }

            List<string> permissionCodeList = new List<string>();
            if (context.HttpContext.User.FindFirst("PermissionCodeList") != null)
            {
                permissionCodeList = JsonConvert.DeserializeObject<List<string>>(context.HttpContext.User.FindFirst("PermissionCodeList").Value) ?? new List<string>();
            }

            // 只要有任一要求的 FuncID 存在於該帳號的 PermissionCode 清單中，即視為授權通過
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

        /// <summary>
        /// Determines whether the specified HTTP request is an AJAX request.
        /// </summary>
        /// 
        /// <returns>
        /// true if the specified HTTP request is an AJAX request; otherwise, false.
        /// </returns>
        /// <param name="request">The HTTP request.</param><exception cref="T:System.ArgumentNullException">The <paramref name="request"/> parameter is null (Nothing in Visual Basic).</exception>
        public static bool IsAjaxRequest(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var a = request.Headers["X-Requested-With"];
            if (request.Headers != null)
                return request.Headers["X-Requested-With"] == "XMLHttpRequest";
            return false;
        }
    }
}
