using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Vehicles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Vehicles.EventHandlers
{
    /// <summary>
    /// Triggered after a Vehicle is soft-deleted.
    /// Extend this handler to cascade cleanup (e.g. unassign drivers, archive route history).
    /// </summary>
    public class VehicleDeletedEventHandler : INotificationHandler<DomainEventNotification<VehicleDeletedDomainEvent>>
    {
        private readonly ILogger<VehicleDeletedEventHandler> _logger;

        public VehicleDeletedEventHandler(ILogger<VehicleDeletedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<VehicleDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogWarning(
                "[Vehicle Deleted] VehicleId={VehicleId} | Plate={LicensePlate}",
                e.VehicleId, e.LicensePlate);

            return Task.CompletedTask;
        }
    }
}
