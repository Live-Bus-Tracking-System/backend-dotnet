using BusTracker.Domain.Common;
using NetTopologySuite.Geometries;

namespace BusTracker.Domain.Entities
{
    public class Stop : AuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public string StopName { get; set; } = string.Empty;
        public Point Location { get; set; } = null!;
    }
}