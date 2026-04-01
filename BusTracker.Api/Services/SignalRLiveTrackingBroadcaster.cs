using BusTracker.Api.Hubs;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Tracking.Models;
using Microsoft.AspNetCore.SignalR;

namespace BusTracker.Api.Services
{
    public class SignalRLiveTrackingBroadcaster : ILiveTrackingBroadcaster
    {
        private readonly IHubContext<TrackingHub> _hub;

        public SignalRLiveTrackingBroadcaster(IHubContext<TrackingHub> hub)
        {
            _hub = hub;
        }

        // Scenario 1 – lightweight card list update
        public Task BroadcastRouteUpdateAsync(RouteBusListDto dto)
            => _hub.Clients
                   .Group(BusTracker.Application.Common.Helpers.GroupNames.Route(dto.RouteId))
                   .SendAsync("ReceiveRouteUpdate", dto);

        // Scenario 2 – text detail (no GPS)
        public Task BroadcastVehicleTextUpdateAsync(VehicleDetailTextDto dto)
            => _hub.Clients
                   .Group(BusTracker.Application.Common.Helpers.GroupNames.VehicleText(dto.VehicleId))
                   .SendAsync("ReceiveVehicleTextUpdate", dto);

        // Scenario 3 – raw GPS frame for the map icon
        public Task BroadcastVehicleMapUpdateAsync(VehicleLiveMapDto dto)
            => _hub.Clients
                   .Group(BusTracker.Application.Common.Helpers.GroupNames.VehicleMap(dto.VehicleId))
                   .SendAsync("ReceiveVehicleMapUpdate", dto);
    }
}
