using BusTracker.Application.Tracking.Models;

namespace BusTracker.Application.Common.Interfaces
{
    public interface IVehicleStateCache
    {
        // Live Bus State
        Task SetStateAsync(string trackerId, VehicleLiveState state);
        Task<VehicleLiveState?> GetStateAsync(string trackerId);
        Task<bool> IsVehicleActiveAsync(string trackerId);

        // Global Route Geometry
        Task<CachedRouteGeometry?> GetRouteGeometryAsync(Guid routeId);
        Task<IEnumerable<CachedRouteGeometry>> GetRouteGeometriesAsync(IEnumerable<Guid> routeIds);
        Task SetRouteGeometryAsync(Guid routeId, CachedRouteGeometry geometry);

        Task<IEnumerable<(string TrackerId, VehicleLiveState State)>> GetAllActiveVehiclesAsync();
    }
}