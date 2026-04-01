using BusTracker.Application.Features.Auth.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Auth.EventHandlers
{
    public class ForgotPasswordEventHandler : INotificationHandler<ForgotPasswordEvent>
    {
        private readonly ILogger<ForgotPasswordEventHandler> _logger;

        public ForgotPasswordEventHandler(ILogger<ForgotPasswordEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ForgotPasswordEvent notification, CancellationToken cancellationToken)
        {
            // Execute background task logic here!
            _logger.LogInformation("Background Task: Send Forgot Password Email to {EmailOrPhone} with Token {Token}.", notification.EmailOrPhone, notification.ResetToken);
            return Task.CompletedTask;
        }
    }
}
