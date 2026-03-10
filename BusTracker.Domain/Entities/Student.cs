using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class Student : AuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;

        public Guid? DefaultRouteId { get; set; }
        public Route? DefaultRoute { get; set; }

        public ICollection<StudentGuardian> Guardians { get; set; } = new List<StudentGuardian>();
    }
}