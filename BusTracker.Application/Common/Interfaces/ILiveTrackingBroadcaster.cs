using BusTracker.Application.Tracking.Models;

namespace BusTracker.Application.Common.Interfaces
{
    /// <summary>
    /// Abstraction over SignalR so the Application layer never references
    /// Microsoft.AspNetCore.SignalR directly (Clean Architecture).
    /// </summary>
    public interface ILiveTrackingBroadcaster
    {
        /// <summary>Scenario 1 – push the lightweight list card to all clients watching the route.</summary>
        Task BroadcastRouteUpdateAsync(RouteBusListDto dto);

        /// <summary>Scenario 2 – push the full stop-detail payload to any client on the text detail screen.</summary>
        Task BroadcastVehicleTextUpdateAsync(VehicleDetailTextDto dto);

        /// <summary>Scenario 3 – push the raw GPS frame to any client on the live map screen.</summary>
        Task BroadcastVehicleMapUpdateAsync(VehicleLiveMapDto dto);
    }
}
