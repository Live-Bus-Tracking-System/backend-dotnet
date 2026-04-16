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

        public string? RegistrationNotes { get; set; }

        public ICollection<ActiveAssignment> AssignmentHistory { get; set; } = new List<ActiveAssignment>();

        public ICollection<VehicleExpectedRoute> ExpectedRoutes { get; set; } = new List<VehicleExpectedRoute>();
        public ICollection<VehiclePermit> Permits { get; set; } = new List<VehiclePermit>();
    }
}