using BusTracker.Application.Features.Auth.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Auth.EventHandlers
{
    public class ResetPasswordEventHandler : INotificationHandler<ResetPasswordEvent>
    {
        private readonly ILogger<ResetPasswordEventHandler> _logger;

        public ResetPasswordEventHandler(ILogger<ResetPasswordEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ResetPasswordEvent notification, CancellationToken cancellationToken)
        {
            // Execute background task logic here!
            _logger.LogInformation("Background Task: Password successfully reset for {EmailOrPhone}.", notification.EmailOrPhone);
            return Task.CompletedTask;
        }
    }
}
