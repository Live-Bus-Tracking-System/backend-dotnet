namespace BusTracker.Application.Tracking.Models
{
    public class LocationPingDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? SpeedKmh { get; set; }
        public double? Heading { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}