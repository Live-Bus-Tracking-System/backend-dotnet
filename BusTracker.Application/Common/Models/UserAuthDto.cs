using System.ComponentModel.DataAnnotations;

namespace BusTracker.Application.Common.Models
{
    public class UserAuthDto
    {
        public string Id { get; init; } = string.Empty;
        public string? Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public IList<string> Roles { get; init; } = new List<string>();
        public IList<string> Permissions { get; init; } = new List<string>();

        [System.Text.Json.Serialization.JsonIgnore]
        public string? SecurityStamp { get; init; }

        public string? OrganizationId { get; init; }
        public string? OrganizationType { get; init; }
    }
}
