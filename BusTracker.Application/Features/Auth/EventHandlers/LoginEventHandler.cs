using BusTracker.Application.Features.Auth.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Auth.EventHandlers
{
    public class LoginEventHandler : INotificationHandler<LoginEvent>
    {
        private readonly ILogger<LoginEventHandler> _logger;

        public LoginEventHandler(ILogger<LoginEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(LoginEvent notification, CancellationToken cancellationToken)
        {
            // Execute background task logic here!
            _logger.LogInformation("Background Task: User {UserId} logged in from IP {IpAddress}.", notification.UserId, notification.IpAddress);
            return Task.CompletedTask;
        }
    }
}
