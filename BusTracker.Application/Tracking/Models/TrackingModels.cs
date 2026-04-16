using BusTracker.Domain.Enums;

namespace BusTracker.Application.Tracking.Models
{
    // THE GLOBAL ROUTE CACHE (One per Route, shared by all buses)
    public class CachedRouteGeometry
    {
        public Guid RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public double MinLat { get; set; }
        public double MaxLat { get; set; }
        public double MinLon { get; set; }
        public double MaxLon { get; set; }
        public List<CachedStop> Stops { get; set; } = new();

        // Exact physical layout of the route mapped via OSRM
        public List<RoutePolylinePoint> PolylineShape { get; set; } = new();

        /// <summary>
        /// Call this exactly ONCE when loading the route from the Database before pushing it to Cache.
        /// This pre-calculates the polyline distances for every stop, keeping the Live-Bus loop
        /// 100% thread-safe and preventing GC allocations.
        /// </summary>
        public void InitializePolylineDistances()
        {
            if (PolylineShape == null || !PolylineShape.Any()) return;

            foreach (var stop in Stops)
            {
                stop.AccumulatedDistanceMeters = BusTracker.Application.Common.Helpers.GeoCalculator.SnapToPolyline(stop.Latitude, stop.Longitude, PolylineShape);
            }
        }
    }

    public class RoutePolylinePoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        // Distance from the very start of the route to this specific coordinate
        public double AccumulatedDistanceMeters { get; set; }
    }

    public class CachedStop
    {
        public Guid StopId { get; set; }
        public int Sequence { get; set; }
        public string StopName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Distance from the start of the route polyline to this stop
        public double AccumulatedDistanceMeters { get; set; }
    }

    // THE LIVE BUS STATE (One per Bus, extremely lightweight)
    public class VehicleLiveState
    {
        public Guid VehicleId { get; set; }
        public string TrackerId { get; set; } = string.Empty;

        // Display fields (set once on cold start, never change)
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;

        // Live Coordinates
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Heading { get; set; } // Derived from Ping 1 to Ping 2 then so on...
        public double? SmoothedSpeedMps { get; set; } = 0;// Exponential moving average of speed
        public DateTime TimestampUtc { get; set; }

        // The Ambiguity Engine
        public bool IsAmbiguous { get; set; }
        public List<Guid> CandidateRouteIds { get; set; } = new();
        /// <summary>
        /// The original expected routes from cold start. Never cleared.
        /// Used to reset CandidateRouteIds on route ejection / cooldown awakening
        /// without re-querying the database.
        /// </summary>
        public List<Guid> ExpectedRouteIds { get; set; } = new();
        public DateTime? CooldownEndsAtUtc { get; set; }

        // The Resolved State
        public Guid? ResolvedRouteId { get; set; }
        public string? RouteName { get; set; }
        public RouteDirection? Direction { get; set; }
        public int LastPassedStopSequence { get; set; }
        public bool IsHardOffline { get; set; } = false;

        // ETAs
        public Dictionary<Guid, DateTime> UpcomingStopEtas { get; set; } = new();

        // The exact distance this vehicle has traveled along the polyline
        public double PolylineAccumulatedDistanceMeters { get; set; }
    }
}