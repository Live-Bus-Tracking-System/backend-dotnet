using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Vehicles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Vehicles.EventHandlers
{
    /// <summary>
    /// Triggered after a Vehicle's details are updated.
    /// Extend this handler to sync external fleet management systems.
    /// </summary>
    public class VehicleUpdatedEventHandler : INotificationHandler<DomainEventNotification<VehicleUpdatedDomainEvent>>
    {
        private readonly ILogger<VehicleUpdatedEventHandler> _logger;

        public VehicleUpdatedEventHandler(ILogger<VehicleUpdatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<VehicleUpdatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogInformation(
                "[Vehicle Updated] VehicleId={VehicleId} | Plate={LicensePlate} | Name={Name}",
                e.VehicleId, e.LicensePlate, e.Name);

            return Task.CompletedTask;
        }
    }
}
