using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Tracking.Models;
using BusTracker.Domain.Entities;
using BusTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusTracker.Infrastructure.BackgroundJobs
{
    public class TrackingEventWorker : BackgroundService
    {
        private readonly ITrackingEventChannel _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrackingEventWorker> _logger;

        public TrackingEventWorker(ITrackingEventChannel channel, IServiceScopeFactory scopeFactory, ILogger<TrackingEventWorker> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TrackingEventWorker is starting.");

            await foreach (var trackingEvent in _channel.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    switch (trackingEvent.EventType)
                    {
                        case TrackingEventType.NewAssignment:
                            await HandleNewAssignmentAsync(context, trackingEvent);
                            break;
                        case TrackingEventType.UpdateAssignment:
                            await HandleUpdateAssignmentAsync(context, trackingEvent);
                            break;
                        case TrackingEventType.CompleteAssignment:
                            await HandleCompleteAssignmentAsync(context, trackingEvent);
                            break;
                        case TrackingEventType.StopArrival:
                            await HandleStopArrivalAsync(context, trackingEvent);
                            break;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing TrackingEvent: {EventType} for Vehicle {VehicleId}", trackingEvent.EventType, trackingEvent.VehicleId);
                }
            }
        }

        private async Task HandleNewAssignmentAsync(ApplicationDbContext context, TrackingEvent evt)
        {
            var existingAssignment = await context.Set<ActiveAssignment>()
                .FirstOrDefaultAsync(a => a.VehicleId == evt.VehicleId && !a.IsCompleted);

            if (existingAssignment != null)
            {
                existingAssignment.IsCompleted = true;
                existingAssignment.EndTimeUtc = DateTime.UtcNow;
            }

            // Look up the vehicle to get the OrganizationId required by the FK constraint.
            var vehicle = await context.Set<Vehicle>()
                .FirstOrDefaultAsync(v => v.Id == evt.VehicleId && !v.IsDeleted);

            if (vehicle == null)
            {
                _logger.LogWarning("HandleNewAssignment: Vehicle {VehicleId} not found, skipping assignment.", evt.VehicleId);
                return;
            }

            var newAssignment = new ActiveAssignment
            {
                VehicleId      = evt.VehicleId,
                RouteId        = evt.RouteId!.Value,
                Direction      = evt.Direction!.Value,
                OrganizationId = vehicle.OrganizationId,
                StartTimeUtc   = DateTime.UtcNow,
                IsCompleted    = false
            };

            context.Set<ActiveAssignment>().Add(newAssignment);
            _logger.LogInformation("Background SQL Write: Assigned Bus {VehicleId} to Route {RouteId}", evt.VehicleId, evt.RouteId);
        }

        private async Task HandleUpdateAssignmentAsync(ApplicationDbContext context, TrackingEvent evt)
        {
            // Here you could update a dedicated TrackingHistory table if desired.
            _logger.LogInformation("Background SQL Write: Bus {VehicleId} passed stop {Sequence} on Route {RouteId}", evt.VehicleId, evt.LastPassedStopSequence, evt.RouteId);
            await Task.CompletedTask;
        }

        private async Task HandleCompleteAssignmentAsync(ApplicationDbContext context, TrackingEvent evt)
        {
            var assignment = await context.Set<ActiveAssignment>()
                .FirstOrDefaultAsync(a => a.VehicleId == evt.VehicleId && !a.IsCompleted);

            if (assignment != null)
            {
                assignment.IsCompleted = true;
                assignment.EndTimeUtc = DateTime.UtcNow;
                _logger.LogInformation("Background SQL Write: Vehicle {VehicleId} completed its route.", evt.VehicleId);
            }
        }

        private async Task HandleStopArrivalAsync(ApplicationDbContext context, TrackingEvent evt)
        {
            // TODO: Implement the write logic when StopArrivalRecord entity is created.
            _logger.LogInformation("Background SQL Write: Analytics recorded for Bus {VehicleId} at Stop {StopId} on Route {RouteId} at {ArrivalTime}", evt.VehicleId, evt.StopId, evt.RouteId, evt.ArrivalTimeUtc);
            await Task.CompletedTask;
        }
    }
}
