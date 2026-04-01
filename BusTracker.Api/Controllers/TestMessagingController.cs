using BusTracker.Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.Api.Controllers
{

    [Route("api/test/messaging")]
    [ApiController]
    public class TestMessagingController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ITemplateService _templateService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TestMessagingController> _logger;

        public TestMessagingController(
            IEmailService emailService,
            ISmsService smsService,
            ITemplateService templateService,
            IWebHostEnvironment env,
            ILogger<TestMessagingController> logger)
        {
            _emailService = emailService;
            _smsService = smsService;
            _templateService = templateService;
            _env = env;
            _logger = logger;
        }


        [HttpPost("email")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request, CancellationToken cancellationToken)
        {
            if (_env.IsProduction())
                return Forbid();

            _logger.LogInformation("[TEST] Firing test email to {To}", request.To);

            var model = new { FullName = request.Name ?? "Test User" };
            var html = await _templateService.RenderTemplateAsync("WelcomeEmail.html", model);

            await _emailService.SendEmailAsync(request.To, "BusTracker — Test Email ✅", html, cancellationToken);

            return Ok(new { Message = $"Email dispatched to {request.To}. Check your inbox (and spam folder)." });
        }


        [HttpPost("sms")]
        public async Task<IActionResult> SendTestSms([FromBody] TestSmsRequest request, CancellationToken cancellationToken)
        {
            if (_env.IsProduction())
                return Forbid();

            _logger.LogInformation("[TEST] Firing test SMS to {To}", request.To);

            await _smsService.SendSmsAsync(request.To, request.Message ?? "Hello from BusTracker! Your Twilio integration is working.", cancellationToken);

            return Ok(new { Message = $"SMS dispatched to {request.To}." });
        }
    }

    public record TestEmailRequest(string To, string? Name);
    public record TestSmsRequest(string To, string? Message);
}
