using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;

namespace BusTracker.Domain.Entities
{
    public class StudentGuardian : AuditableEntity
    {
        public Guid StudentId { get; set; }
        public Student? Student { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string GuardianName { get; set; } = string.Empty;
        public string? NormalizedEmail { get; set; }
        public string NormalizedPhoneNumber { get; set; } = string.Empty;
        public string? Relationship { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public string? SystemNotes { get; set; }
    }
}