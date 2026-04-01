using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;

namespace BusTracker.Domain.Entities
{
    public class VehiclePermit : AuditableEntity
    {
        public Guid VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public Guid? RouteId { get; set; }
        public Route? Route { get; set; }

        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public PermitStatus PermitStatus { get; set; } = PermitStatus.Pending;

        public string? PermitNumber { get; set; }

        public DateTime? VerifiedAtUtc { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public string? ApprovedBy { get; set; }

        public string? Notes { get; set; }
    }
}
