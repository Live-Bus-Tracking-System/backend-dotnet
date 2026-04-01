using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace BusTracker.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object?>
            {
                Success = false,
                Meta    = new ApiMeta { RequestId = context.TraceIdentifier }
            };

            switch (exception)
            {
                case CustomValidationException validationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    // Flatten dictionary into a simple list of error strings for the ApiResponse format
                    var validationErrors = validationException.Errors
                        .SelectMany(kvp => kvp.Value.Select(err => $"{kvp.Key}: {err}"))
                        .ToList();
                    
                    response = new ApiResponse<object?>
                    {
                        Success = false,
                        Message = "Validation Failed.",
                        Errors  = validationErrors,
                        Meta    = new ApiMeta { RequestId = context.TraceIdentifier }
                    };
                    break;
                
                case NotFoundException notFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = new ApiResponse<object?>
                    {
                        Success = false,
                        Message = notFoundException.Message,
                        Errors  = { notFoundException.Message },
                        Meta    = new ApiMeta { RequestId = context.TraceIdentifier }
                    };
                    break;
                
                case UnauthorizedException unauthorizedException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new ApiResponse<object?>
                    {
                        Success = false,
                        Message = "Authentication Failed.",
                        Errors  = { unauthorizedException.Message },
                        Meta    = new ApiMeta { RequestId = context.TraceIdentifier }
                    };
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new ApiResponse<object?>
                    {
                        Success = false,
                        Message = "An internal server error occurred.",
                        Errors  = { "Internal Server Error" },
                        Meta    = new ApiMeta { RequestId = context.TraceIdentifier }
                    };
                    break;
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return context.Response.WriteAsync(json);
        }
    }
}
