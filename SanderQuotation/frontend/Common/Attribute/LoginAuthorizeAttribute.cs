using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static Const.Enums;

namespace frontend.Common.Attribute
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CustomAuthorizationAttribute : ActionFilterAttribute
    {

        public IEnumerable<int> functions { get; set; }

        public CustomAuthorizationAttribute(params FuncID[] functions)
        {
            this.functions = functions.Select(x => (int)x);
            //this.Order = 2;   // 必須在登入驗證之後執行
        }


        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //if (LoginSession.Current == null || string.IsNullOrEmpty(LoginSession.Current.empno))
            //{
            //    {
            //        if (IsAjaxRequest(context.HttpContext.Request))//是Ajax的話
            //        {
            //            context.HttpContext.Response.StatusCode = 440;//Login timeout
            //            context.Result = new JsonResult(new { Success = false });
            //            //context.HttpContext.Response..End();
            //        }
            //        else
            //        {
            //            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            //            String loginUrl = config["loginUrl"];
            //            context.Result = new RedirectResult(loginUrl);
 
            //        }
            //    }
            //}

            //if (LoginSession.Current != null && !string.IsNullOrEmpty(LoginSession.Current.empno))
            //{
            //    //重置密碼頁   不需要權限
            //    if (functions != null && functions.Any() && functions.Where(w => w == (int)FuncID.ResetTESTP || w == (int)FuncID.Home_View).Count() > 0)
            //    {

            //    }
            //    else if (functions == null || !functions.Any() || !LoginSession.Authorization(functions))
            //    {
            //        if (IsAjaxRequest(context.HttpContext.Request))//是Ajax的話
            //        {
            //            context.HttpContext.Response.StatusCode = 403;//Login timeout
            //            context.Result = new JsonResult(new { Success = false });
            //            //context.HttpContext.Response..End();
            //        }
            //        else
            //        {
            //            {
            //                context.Result = new RedirectToRouteResult(new RouteValueDictionary
            //                {
            //                    { "action", "PermissionDenied" },
            //                    { "controller", "Home" },
            //                     { "area", string.Empty }
            //                });
            //            }


            //        }
            //    }
            //}
        }

        /// <summary>
        /// Determines whether the specified HTTP request is an AJAX request.
        /// </summary>
        /// 
        /// <returns>
        /// true if the specified HTTP request is an AJAX request; otherwise, false.
        /// </returns>
        /// <param name="request">The HTTP request.</param><exception cref="T:System.ArgumentNullException">The <paramref name="request"/> parameter is null (Nothing in Visual Basic).</exception>
        public bool IsAjaxRequest(HttpRequest request)
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
