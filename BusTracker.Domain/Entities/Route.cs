using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class Route : AuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public string RouteNumber { get; set; } = string.Empty;

        public string OriginName { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;

        public string RouteName => $"{OriginName} -> {DestinationName}";

        public bool IsPublic { get; set; } = true;
    }
}