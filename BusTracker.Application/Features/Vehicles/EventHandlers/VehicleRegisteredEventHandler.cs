using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Vehicles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Vehicles.EventHandlers
{
    /// <summary>
    /// Triggered after a new Vehicle is registered.
    /// Extend this handler to notify staff or trigger compliance checks.
    /// </summary>
    public class VehicleRegisteredEventHandler : INotificationHandler<DomainEventNotification<VehicleRegisteredDomainEvent>>
    {
        private readonly ILogger<VehicleRegisteredEventHandler> _logger;

        public VehicleRegisteredEventHandler(ILogger<VehicleRegisteredEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<VehicleRegisteredDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogInformation(
                "[Vehicle Registered] VehicleId={VehicleId} | Plate={LicensePlate} | Name={Name} | OrgId={OrganisationId} | By={UserId}",
                e.VehicleId, e.LicensePlate, e.Name, e.OrganisationId, e.CreatedByUserId);

            return Task.CompletedTask;
        }
    }
}
