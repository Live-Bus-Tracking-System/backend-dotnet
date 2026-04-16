using BusTracker.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BusTracker.Api.Filters
{
    public class ApiResponseFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
                return;

            switch (context.Result)
            {
                case ObjectResult { Value: ApiResponse<object?> }:
                    return;

                // Validation errors (400 from model binding / FluentValidation)
                case BadRequestObjectResult { Value: ValidationProblemDetails problems }:
                    {
                        var errors = problems.Errors.SelectMany(kv => kv.Value).ToList();
                        var wrapped = ApiResponse.Fail("Validation failed.", errors);
                        context.Result = new BadRequestObjectResult(wrapped);
                        break;
                    }

                // Success: wrap the raw object value
                case ObjectResult objectResult:
                    {
                        var requestId = context.HttpContext.TraceIdentifier;
                        var wrapped = new ApiResponse<object?>
                        {
                            Success = objectResult.StatusCode is null or >= 200 and < 300,
                            Message = objectResult.StatusCode is null or >= 200 and < 300
                                ? "Success"
                                : "An error occurred.",
                            Data = objectResult.Value,
                            Meta = new ApiMeta { RequestId = requestId }
                        };
                        context.Result = new ObjectResult(wrapped)
                        {
                            StatusCode = objectResult.StatusCode
                        };
                        break;
                    }

                // Empty 204 No Content — wrap with a success message
                case EmptyResult:
                    {
                        var wrapped = ApiResponse.Ok("Operation completed successfully.");
                        context.Result = new OkObjectResult(wrapped);
                        break;
                    }
            }
        }
    }
}
