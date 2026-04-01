//namespace BusTracker.Application.Tracking.Models
//{
//    public class VehicleLocationState
//    {
//        public Guid VehicleId { get; set; }
//        public string TrackerId { get; set; } = string.Empty;
//        public Guid? ActiveRouteId { get; set; }

//        // Current Ping Data
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }
//        public double? SpeedKmh { get; set; }
//        public DateTime TimestampUtc { get; set; }

//        // Geofencing State
//        public int LastPassedStopSequence { get; set; }

//        // The In-Memory Map (Crucial for bypassing SQL!)
//        public List<CachedStop> RouteStops { get; set; } = new List<CachedStop>();
//    }

//    public class CachedStop
//    {
//        public Guid StopId { get; set; }
//        public int Sequence { get; set; }
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }
//    }
//}