using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class Vehicle : AuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public string TrackerId { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int? Capacity { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<ActiveAssignment> AssignmentHistory { get; set; } = new List<ActiveAssignment>();
    }
}