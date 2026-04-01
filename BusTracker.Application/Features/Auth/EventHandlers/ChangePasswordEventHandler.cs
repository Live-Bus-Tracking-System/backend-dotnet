using BusTracker.Application.Features.Auth.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Auth.EventHandlers
{
    public class ChangePasswordEventHandler : INotificationHandler<ChangePasswordEvent>
    {
        private readonly ILogger<ChangePasswordEventHandler> _logger;

        public ChangePasswordEventHandler(ILogger<ChangePasswordEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ChangePasswordEvent notification, CancellationToken cancellationToken)
        {
            // Execute background task logic here!
            _logger.LogInformation("Background Task: User {UserId} explicitly successfully modified their password.", notification.UserId);
            return Task.CompletedTask;
        }
    }
}
