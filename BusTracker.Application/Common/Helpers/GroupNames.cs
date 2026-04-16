using System;

namespace BusTracker.Application.Common.Helpers
{
    /// <summary>
    /// Centralised group name factory — keeps Hub and Service in sync.
    /// </summary>
    public static class GroupNames
    {
        public static string Route(Guid routeId) => $"Route_{routeId.ToString("D").ToLowerInvariant()}";
        public static string VehicleText(Guid vehicleId) => $"Vehicle_Text_{vehicleId.ToString("D").ToLowerInvariant()}";
        public static string VehicleMap(Guid vehicleId) => $"Vehicle_Map_{vehicleId.ToString("D").ToLowerInvariant()}";
    }
}
