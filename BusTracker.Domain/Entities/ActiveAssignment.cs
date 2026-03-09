using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;

namespace BusTracker.Domain.Entities
{
    public class ActiveAssignment : AuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public Guid VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public Guid RouteId { get; set; }
        public Route? Route { get; set; }

        public RouteDirection Direction { get; set; }

        public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndTimeUtc { get; set; }

        public bool IsCompleted { get; set; } = false;
    }
}