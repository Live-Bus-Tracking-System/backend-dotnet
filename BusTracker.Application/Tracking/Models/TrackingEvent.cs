using BusTracker.Domain.Enums;

namespace BusTracker.Application.Tracking.Models
{
    public enum TrackingEventType
    {
        UpdateAssignment,
        NewAssignment,
        CompleteAssignment,
        StopArrival
    }

    public class TrackingEvent
    {
        public TrackingEventType EventType { get; set; }
        public Guid VehicleId { get; set; }
        public Guid? RouteId { get; set; }
        public int? LastPassedStopSequence { get; set; }
        public RouteDirection? Direction { get; set; }
        public Guid? StopId { get; set; }
        public DateTime? ArrivalTimeUtc { get; set; }
    }
}
