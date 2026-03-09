using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class Student : AuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string AdmissionNumber { get; set; } = string.Empty;

        public string? SecretPIN { get; set; }

        public Guid? DefaultRouteId { get; set; }
        public Route? DefaultRoute { get; set; }
    }
}