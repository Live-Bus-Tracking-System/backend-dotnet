using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Organisations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Organisations.EventHandlers
{
    /// <summary>
    /// Triggered after an Organisation is suspended by a SuperAdmin.
    /// Extend this handler to notify the Org Admin of the suspension and reason.
    /// </summary>
    public class OrganisationSuspendedEventHandler : INotificationHandler<DomainEventNotification<OrganisationSuspendedDomainEvent>>
    {
        private readonly ILogger<OrganisationSuspendedEventHandler> _logger;

        public OrganisationSuspendedEventHandler(ILogger<OrganisationSuspendedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<OrganisationSuspendedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogWarning(
                "[Org Suspended] OrganisationId={OrganisationId} | Name={Name} | Email={Email} | Reason={Reason}",
                e.OrganisationId, e.Name, e.Email, e.Reason ?? "No reason provided");

            // TODO: Bulk SecurityStamp bump — inject UserManager<ApplicationUser> and IApplicationDbContext,
            // fetch all users where OrganizationId == e.OrganisationId, and call
            // UpdateSecurityStampAsync(user) for each. This ensures every org member's
            // refresh token is rejected at next rotation, killing all active sessions.

            return Task.CompletedTask;
        }
    }
}
