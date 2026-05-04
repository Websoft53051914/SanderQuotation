using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace backend.Common
{
    public class ApiSystemLockFilter : IAsyncActionFilter
    {
        private readonly ISystemLockService _lockService;

        public ApiSystemLockFilter(ISystemLockService lockService)
        {
            _lockService = lockService;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var userId = context.HttpContext.User.Identity?.Name;

            if (!string.IsNullOrEmpty(userId) &&
                _lockService.IsLocked(userId))
            {
                context.Result = new ObjectResult(new
                {
                    locked = true,
                    message = "System is locked"
                })
                {
                    StatusCode = StatusCodes.Status423Locked
                };
                return;
            }

            await next();
        }
    }

}
