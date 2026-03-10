using System.ComponentModel.DataAnnotations;

namespace BusTracker.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}