using Microsoft.AspNetCore.Identity;

namespace BusTracker.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }

        // Navigation Property for Entity Framework
        public Organization? Organization { get; set; }
    }
}