namespace BusTracker.Domain.Common
{
    public abstract class AuditableEntity : BaseEntity
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? LastModifiedAtUtc { get; set; }
        public string? LastModifiedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}