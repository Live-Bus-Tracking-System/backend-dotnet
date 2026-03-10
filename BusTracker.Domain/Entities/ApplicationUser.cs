using Microsoft.AspNetCore.Identity;

namespace BusTracker.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public ICollection<StudentGuardian> StudentLinks { get; set; } = new List<StudentGuardian>();
    }
}