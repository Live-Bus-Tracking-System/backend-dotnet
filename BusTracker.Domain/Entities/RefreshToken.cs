using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAtUtc { get; set; }

        public string? ReplacedByTokenHash { get; set; }

        public string? SecurityStamp { get; set; }

        public Guid? FamilyId { get; set; }

        public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAtUtc;
    }
}
