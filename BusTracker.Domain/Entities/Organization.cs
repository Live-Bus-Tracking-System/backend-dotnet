using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;

namespace BusTracker.Domain.Entities
{
    public class Organization : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public OrganizationType Type { get; set; }
        public string? InviteCode { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}