using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Organisations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Organisations.EventHandlers
{
    /// <summary>
    /// Triggered after an Organisation is soft-deleted by a SuperAdmin.
    /// Extend this handler to trigger cleanup tasks (e.g. cascade deactivation of staff accounts).
    /// </summary>
    public class OrganisationDeletedEventHandler : INotificationHandler<DomainEventNotification<OrganisationDeletedDomainEvent>>
    {
        private readonly ILogger<OrganisationDeletedEventHandler> _logger;

        public OrganisationDeletedEventHandler(ILogger<OrganisationDeletedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<OrganisationDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogWarning(
                "[Org Deleted] OrganisationId={OrganisationId} | Name={Name} | Email={Email}",
                e.OrganisationId, e.Name, e.Email);

            return Task.CompletedTask;
        }
    }
}
