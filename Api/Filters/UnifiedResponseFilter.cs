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