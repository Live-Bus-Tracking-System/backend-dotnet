using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;
using NetTopologySuite.Geometries;

namespace BusTracker.Domain.Entities
{
    public class Stop : AuditableEntity
    {
        public Guid? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public string StopName { get; set; } = string.Empty;
        public Point Location { get; set; } = null!;

        public bool IsGlobal { get; set; } = false;
        public DataOrigin DataOrigin { get; set; } = DataOrigin.Manual;
    }
}