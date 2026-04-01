using BusTracker.Application.Tracking.Models;
using BusTracker.Domain.Enums;

namespace BusTracker.Application.Common.Interfaces.Repository
{
    public interface ITrackingRepository
    {
        Task<VehicleLiveState?> InitializeColdStateAsync(string trackerId);
        Task QueueActiveAssignmentUpdateAsync(Guid vehicleId, Guid routeId, int lastPassedStopSequence);
        Task QueueNewActiveAssignmentAsync(Guid vehicleId, Guid routeId, RouteDirection direction);
        Task QueueActiveAssignmentCompletionAsync(Guid vehicleId);
        Task QueueStopArrivalRecordAsync(Guid vehicleId, Guid routeId, Guid stopId, DateTime arrivalTimeUtc);
        Task<CachedRouteGeometry?> BuildRouteGeometryFromSqlAsync(Guid routeId);
    }
}
