using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Organisations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Organisations.EventHandlers
{
    /// <summary>
    /// Triggered after an Organisation is activated by a SuperAdmin.
    /// Extend this handler to send an activation confirmation email to the Org Admin.
    /// </summary>
    public class OrganisationActivatedEventHandler : INotificationHandler<DomainEventNotification<OrganisationActivatedDomainEvent>>
    {
        private readonly ILogger<OrganisationActivatedEventHandler> _logger;

        public OrganisationActivatedEventHandler(ILogger<OrganisationActivatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<OrganisationActivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogInformation(
                "[Org Activated] OrganisationId={OrganisationId} | Name={Name} | Email={Email}",
                e.OrganisationId, e.Name, e.Email);

            return Task.CompletedTask;
        }
    }
}
