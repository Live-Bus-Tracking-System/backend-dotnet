using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Application.Tracking.Models;
using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusTracker.Infrastructure.Persistence.Repositories
{
    public class TrackingRepository : ITrackingRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrackingRepository> _logger;
        private readonly ITrackingEventChannel _channel;

        public TrackingRepository(ApplicationDbContext context, IServiceScopeFactory scopeFactory, ILogger<TrackingRepository> logger, ITrackingEventChannel channel)
        {
            _context = context;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _channel = channel;
        }

        public async Task<VehicleLiveState?> InitializeColdStateAsync(string trackerId)
        {
            var vehicle = await _context.Set<Vehicle>()
                .Include(v => v.ExpectedRoutes)
                .Include(v => v.AssignmentHistory.Where(a => !a.IsCompleted))
                .FirstOrDefaultAsync(v => v.TrackerId == trackerId && !v.IsDeleted);

            if (vehicle == null) return null;

            var activeAssignment = vehicle.AssignmentHistory.FirstOrDefault();

            return new VehicleLiveState
            {
                VehicleId = vehicle.Id,
                TrackerId = vehicle.TrackerId,
                VehicleName = vehicle.Name ?? null,
                LicensePlate = vehicle.LicensePlate,
                ResolvedRouteId = activeAssignment?.RouteId,
                Direction = activeAssignment?.Direction,

                // If there is no active assignment, but they have expected routes, trigger Ambiguity Engine!
                IsAmbiguous = activeAssignment == null && vehicle.ExpectedRoutes.Any(),
                CandidateRouteIds = activeAssignment == null
                    ? vehicle.ExpectedRoutes.Select(er => er.RouteId).ToList()
                    : new List<Guid>(),

                LastPassedStopSequence = 0,
                TimestampUtc = DateTime.MinValue
            };
        }

        public async Task QueueActiveAssignmentUpdateAsync(Guid vehicleId, Guid routeId, int lastPassedStopSequence)
        {
            _channel.TryWrite(new TrackingEvent
            {
                EventType = TrackingEventType.UpdateAssignment,
                VehicleId = vehicleId,
                RouteId = routeId,
                LastPassedStopSequence = lastPassedStopSequence
            });
            await Task.CompletedTask;
        }

        public async Task QueueNewActiveAssignmentAsync(Guid vehicleId, Guid routeId, RouteDirection direction)
        {
            _channel.TryWrite(new TrackingEvent
            {
                EventType = TrackingEventType.NewAssignment,
                VehicleId = vehicleId,
                RouteId = routeId,
                Direction = direction
            });
            await Task.CompletedTask;
        }

        public async Task QueueActiveAssignmentCompletionAsync(Guid vehicleId)
        {
            _channel.TryWrite(new TrackingEvent
            {
                EventType = TrackingEventType.CompleteAssignment,
                VehicleId = vehicleId
            });
            await Task.CompletedTask;
        }

        public async Task QueueStopArrivalRecordAsync(Guid vehicleId, Guid routeId, Guid stopId, DateTime arrivalTimeUtc)
        {
            _channel.TryWrite(new TrackingEvent
            {
                EventType = TrackingEventType.StopArrival,
                VehicleId = vehicleId,
                RouteId = routeId,
                StopId = stopId,
                ArrivalTimeUtc = arrivalTimeUtc
            });
            await Task.CompletedTask;
        }

        public async Task<CachedRouteGeometry?> BuildRouteGeometryFromSqlAsync(Guid routeId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var route = await context.Set<Route>()
                .Include(r => r.RouteStops)
                .ThenInclude(rs => rs.Stop)
                .FirstOrDefaultAsync(r => r.Id == routeId && !r.IsDeleted);

            if (route == null) return null;

            var geometry = new CachedRouteGeometry
            {
                RouteId = route.Id,
                RouteName = route.RouteNumber ?? route.Id.ToString(),
                Stops = route.RouteStops.OrderBy(rs => rs.StopSequence).Select(rs => new CachedStop
                {
                    StopId = rs.StopId,
                    Sequence = rs.StopSequence,
                    StopName = rs.Stop!.StopName,
                    Latitude = rs.Stop.Location.Y,
                    Longitude = rs.Stop.Location.X,
                    AccumulatedDistanceMeters = 0
                }).ToList()
            };

            if (!string.IsNullOrEmpty(route.RouteShapeCoordinates))
            {
                geometry.PolylineShape = BusTracker.Application.Common.Helpers.GeoCalculator.DecodePolyline(route.RouteShapeCoordinates);
            }

            if (geometry.Stops.Any())
            {
                geometry.MinLat = geometry.Stops.Min(s => s.Latitude);
                geometry.MaxLat = geometry.Stops.Max(s => s.Latitude);
                geometry.MinLon = geometry.Stops.Min(s => s.Longitude);
                geometry.MaxLon = geometry.Stops.Max(s => s.Longitude);
            }

            if (geometry.PolylineShape != null && geometry.PolylineShape.Any())
            {
                geometry.MinLat = Math.Min(geometry.MinLat, geometry.PolylineShape.Min(p => p.Latitude));
                geometry.MaxLat = Math.Max(geometry.MaxLat, geometry.PolylineShape.Max(p => p.Latitude));
                geometry.MinLon = Math.Min(geometry.MinLon, geometry.PolylineShape.Min(p => p.Longitude));
                geometry.MaxLon = Math.Max(geometry.MaxLon, geometry.PolylineShape.Max(p => p.Longitude));
            }

            return geometry;
        }
    }
}