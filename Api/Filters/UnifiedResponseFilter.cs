using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters
{
    public class UnifiedResponseFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // n8n / tools HTTP: ?raw=1 o header X-Raw-Response evita el wrapper {success,data}
            var request = context.HttpContext.Request;
            var wantsRaw =
                request.Query.ContainsKey("raw")
                || string.Equals(request.Headers["X-Raw-Response"], "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Headers["X-Raw-Response"], "true", StringComparison.OrdinalIgnoreCase);

            if (wantsRaw)
                return;

            if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;

                if (statusCode >= 200 && statusCode < 300)
                {
                    objectResult.Value = new
                    {
                        success = true,
                        data = objectResult.Value,
                    };
                }
            }
            else if (context.Result is NoContentResult)
            {
                context.Result = new ObjectResult(new { success = true, data = (object?)null })
                {
                    StatusCode = StatusCodes.Status200OK,
                };
            }
        }
    }
}