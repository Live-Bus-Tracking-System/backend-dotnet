using BusTracker.Application.Features.Auth.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using BusTracker.Application.Common.Interfaces.Services;

namespace BusTracker.Application.Features.Auth.EventHandlers
{
    public class RegisterEventHandler : INotificationHandler<RegisterEvent>
    {
        private readonly ILogger<RegisterEventHandler> _logger;
        private readonly IEmailService _emailService;
        private readonly ITemplateService _templateService;

        public RegisterEventHandler(ILogger<RegisterEventHandler> logger, IEmailService emailService, ITemplateService templateService)
        {
            _logger = logger;
            _emailService = emailService;
            _templateService = templateService;
        }

        public async Task Handle(RegisterEvent notification, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(notification.Email))
            {
                return;
            }

            _logger.LogInformation("Background Task: Starting email generation for newly registered user {FullName} ({Email}).", notification.FullName, notification.Email);

            try
            {
                var model = new { FullName = notification.FullName };

                var htmlBody = await _templateService.RenderTemplateAsync("WelcomeEmail.html", model);

                await _emailService.SendEmailAsync(notification.Email, "Welcome to BusTracker!", htmlBody, cancellationToken);
                
                _logger.LogInformation("Successfully sent Welcome email to {Email}.", notification.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to completely send welcome email to {Email}", notification.Email);
                throw;
            }
        }
    }
}
