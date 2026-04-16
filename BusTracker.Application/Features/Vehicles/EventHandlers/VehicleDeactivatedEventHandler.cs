using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Vehicles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Vehicles.EventHandlers
{
    /// <summary>
    /// Triggered after a Vehicle is deactivated.
    /// Extend this handler to cancel active route assignments or notify drivers.
    /// </summary>
    public class VehicleDeactivatedEventHandler : INotificationHandler<DomainEventNotification<VehicleDeactivatedDomainEvent>>
    {
        private readonly ILogger<VehicleDeactivatedEventHandler> _logger;

        public VehicleDeactivatedEventHandler(ILogger<VehicleDeactivatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<VehicleDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogWarning(
                "[Vehicle Deactivated] VehicleId={VehicleId} | Plate={LicensePlate}",
                e.VehicleId, e.LicensePlate);

            return Task.CompletedTask;
        }
    }
}
