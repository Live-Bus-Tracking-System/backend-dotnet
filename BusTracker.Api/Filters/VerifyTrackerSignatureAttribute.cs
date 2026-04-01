using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;

namespace BusTracker.Api.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class VerifyTrackerSignatureAttribute : Attribute, IAsyncResourceFilter
    {
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            if (!request.Headers.TryGetValue("X-Tracker-Id", out var trackerId) ||
                !request.Headers.TryGetValue("X-Signature", out var signature))
            {
                context.Result = new UnauthorizedObjectResult("Missing tracking headers.");
                return;
            }

            // MUST call this BEFORE model binding reads the stream
            request.EnableBuffering();

            // Read the raw body bytes for HMAC verification
            string rawPayload;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                request.Body.Position = 0; // Seek back to start after EnableBuffering
                rawPayload = await reader.ReadToEndAsync();
                request.Body.Position = 0; // Reset for the Model Binder to read it next
            }

            var securityService = context.HttpContext.RequestServices.GetRequiredService<ITrackerSecurityService>();

            if (!securityService.IsSignatureValid(trackerId.ToString(), rawPayload, signature.ToString()))
            {
                context.Result = new UnauthorizedObjectResult("Cryptographic signature validation failed.");
                return;
            }

            await next();
        }
    }
}