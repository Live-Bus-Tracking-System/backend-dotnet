using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Organisations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Organisations.EventHandlers
{
    /// <summary>
    /// Triggered after an Organisation's details are updated.
    /// Extend this handler to notify affected users or sync external systems.
    /// </summary>
    public class OrganisationUpdatedEventHandler : INotificationHandler<DomainEventNotification<OrganisationUpdatedDomainEvent>>
    {
        private readonly ILogger<OrganisationUpdatedEventHandler> _logger;

        public OrganisationUpdatedEventHandler(ILogger<OrganisationUpdatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<OrganisationUpdatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogInformation(
                "[Org Updated] OrganisationId={OrganisationId} | Name={Name} | Email={Email}",
                e.OrganisationId, e.Name, e.Email);

            return Task.CompletedTask;
        }
    }
}
