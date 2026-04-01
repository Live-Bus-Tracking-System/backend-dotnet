using BusTracker.Domain.Enums;

namespace BusTracker.Application.Tracking.Models
{
    // ─────────────────────────────────────────────────────────────────────────
    // SCENARIO 1: Route Bus List Screen
    // No GPS. Ultra-lightweight push to the Route group.
    // Frontend: scrolling card list per bus on a selected route.
    // ─────────────────────────────────────────────────────────────────────────
    public class RouteBusListDto
    {
        public Guid VehicleId { get; set; }
        public Guid RouteId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public RouteDirection? Direction { get; set; }

        // Immediate next stop only
        public string? NextStopName { get; set; }
        public DateTime? NextStopEtaUtc { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCENARIO 2: Bus Stop Detail Screen
    // No GPS. Heavy text payload with stop sequence, ETA, and distance string.
    // Frontend: full scrollable list of all upcoming stops for a single bus.
    // ─────────────────────────────────────────────────────────────────────────
    public class VehicleDetailTextDto
    {
        public Guid VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public RouteDirection? Direction { get; set; }

        // Promoted next stop for the card header
        public string? NextStopName { get; set; }
        public DateTime? NextStopEtaUtc { get; set; }

        // All remaining stops
        public List<UpcomingStopDetailDto> UpcomingStops { get; set; } = new();
    }

    public class UpcomingStopDetailDto
    {
        public Guid StopId { get; set; }
        public int Sequence { get; set; }
        public string StopName { get; set; } = string.Empty;
        public DateTime EtaUtc { get; set; }

        // Pre-formatted distance string so the frontend does zero maths
        public string DistanceText { get; set; } = string.Empty; // e.g. "1.2 km"
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCENARIO 3: Live Map Tracking Screen
    // GPS-Heavy, text-light. Drives the moving bus icon on the Leaflet map.
    // Frontend: map + small overlay card with next stop.
    // ─────────────────────────────────────────────────────────────────────────
    public class VehicleLiveMapDto
    {
        public Guid VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public RouteDirection? Direction { get; set; }

        // Raw GPS — what the map icon consumes every ping
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Heading { get; set; }   // Rotates the bus icon on the map
        public double? SpeedKph { get; set; }  // Displayed in the info chip

        // Overlay card on the map
        public string? NextStopName { get; set; }
        public DateTime? NextStopEtaUtc { get; set; }
    }
}
