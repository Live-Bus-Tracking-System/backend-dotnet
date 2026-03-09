using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class RouteStop : AuditableEntity
    {
        public Guid RouteId { get; set; }
        public Route? Route { get; set; }

        public Guid StopId { get; set; }
        public Stop? Stop { get; set; }

        public int StopSequence { get; set; }

        public double? DistanceToNextStopMeters { get; set; }
    }
}