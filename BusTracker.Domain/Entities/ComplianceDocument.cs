using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;

namespace BusTracker.Domain.Entities
{
    public class ComplianceDocument : AuditableEntity
    {
        public ComplianceDocumentEntityType EntityType { get; set; }
        public Guid EntityId { get; set; }

        public ComplianceDocumentType DocumentType { get; set; }

        public string DocumentUrl { get; set; } = string.Empty;

        public string? DocumentNumber { get; set; }
        public string? IssuedBy { get; set; }
        public DateOnly? IssuedAtDate { get; set; }
        public DateOnly? ExpiresAtDate { get; set; }

        public bool IsVerified { get; set; } = false;
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAtUtc { get; set; }

        public string? Notes { get; set; }
    }
}
