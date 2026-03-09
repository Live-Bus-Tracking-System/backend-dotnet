using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class StudentGuardian : AuditableEntity
    {
        public Guid StudentId { get; set; }
        public Student? Student { get; set; }

        public string GuardianId { get; set; } = string.Empty;
        public ApplicationUser? Guardian { get; set; }

        public bool IsApproved { get; set; } = false;

        // Why it was approved/rejected (e.g., "Auto-matched via PIN" or "Manual Approval")
        public string? ApprovalNotes { get; set; }
    }
}