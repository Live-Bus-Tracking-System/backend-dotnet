using BusTracker.Application.Common.Events;
using BusTracker.Domain.Events.Vehicles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BusTracker.Application.Features.Vehicles.EventHandlers
{
    /// <summary>
    /// Triggered after a Vehicle is activated.
    /// Extend this handler to alert the operations team or un-pause route assignments.
    /// </summary>
    public class VehicleActivatedEventHandler : INotificationHandler<DomainEventNotification<VehicleActivatedDomainEvent>>
    {
        private readonly ILogger<VehicleActivatedEventHandler> _logger;

        public VehicleActivatedEventHandler(ILogger<VehicleActivatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<VehicleActivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;
            _logger.LogInformation(
                "[Vehicle Activated] VehicleId={VehicleId} | Plate={LicensePlate}",
                e.VehicleId, e.LicensePlate);

            return Task.CompletedTask;
        }
    }
}
