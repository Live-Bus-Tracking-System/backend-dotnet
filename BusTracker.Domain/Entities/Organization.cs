using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;

namespace BusTracker.Domain.Entities
{
    public class Organization : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public string NormalizedPhoneNumber { get; set; } = string.Empty;
        public OrganizationType Type { get; set; }
        public string? HashedInviteCode { get; set; }
        public DateTime? InviteCodeExpiresAtUtc { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}