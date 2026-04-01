using BusTracker.Application.Common.Helpers;
using BusTracker.Application.Tracking.Models;
using Microsoft.AspNetCore.SignalR;
using BusTracker.Domain.Enums;

namespace BusTracker.Api.Hubs
{
    public class TrackingHub : Hub
    {
        // ── SCENARIO 1: Route Bus List ─────────────────────────────────
        // Call when the user opens the bus list for a route.

        public async Task JoinRouteGroup(Guid routeId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, BusTracker.Application.Common.Helpers.GroupNames.Route(routeId));

        public async Task LeaveRouteGroup(Guid routeId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, BusTracker.Application.Common.Helpers.GroupNames.Route(routeId));

        // ── SCENARIO 2: Bus Stop Detail Text ──────────────────────────
        // Call when the user taps a bus card and enters the detail screen.

        public async Task JoinVehicleTextGroup(Guid vehicleId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, BusTracker.Application.Common.Helpers.GroupNames.VehicleText(vehicleId));

        public async Task LeaveVehicleTextGroup(Guid vehicleId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, BusTracker.Application.Common.Helpers.GroupNames.VehicleText(vehicleId));

        // ── SCENARIO 3: Live Map Tracking ─────────────────────────────
        // Call ONLY when the Leaflet/Google Map component is mounted.

        public override async Task OnConnectedAsync()
            => await base.OnConnectedAsync();

        public override async Task OnDisconnectedAsync(Exception? exception)
            => await base.OnDisconnectedAsync(exception);

        public async Task JoinVehicleMapGroup(Guid vehicleId)
        {
            var groupName = BusTracker.Application.Common.Helpers.GroupNames.VehicleMap(vehicleId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveVehicleMapGroup(Guid vehicleId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, BusTracker.Application.Common.Helpers.GroupNames.VehicleMap(vehicleId));
    }

}
