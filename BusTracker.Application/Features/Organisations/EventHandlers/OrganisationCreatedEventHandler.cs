using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Organisations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Organisations.EventHandlers
{
    /// <summary>
    /// Triggered after a new Organisation is persisted.
    /// Extend this handler to send a welcome/onboarding email to the creating admin.
    /// </summary>
    public class OrganisationCreatedEventHandler : INotificationHandler<DomainEventNotification<OrganisationCreatedDomainEvent>>
    {
        private readonly ILogger<OrganisationCreatedEventHandler> _logger;

        public OrganisationCreatedEventHandler(ILogger<OrganisationCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<OrganisationCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogInformation(
                "[Org Created] OrganisationId={OrganisationId} | Name={Name} | Email={Email} | CreatedBy={CreatedByUserId}",
                e.OrganisationId, e.Name, e.Email, e.CreatedByUserId);

            return Task.CompletedTask;
        }
    }
}
