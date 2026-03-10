using BusTracker.Domain.Common;
using BusTracker.Domain.Enums;
using System.Linq;

namespace BusTracker.Domain.Entities
{
    public class Route : AuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public string? RouteNumber { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = true;

        public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();

        // Dynamic route name calculation. 
        // Note: Application layer MUST use .Include(r => r.RouteStops).ThenInclude(rs => rs.Stop) 
        // when fetching routes from the DB for this to work!
        public string GetRouteName(RouteDirection direction = RouteDirection.Outbound)
        {
            if (RouteStops == null || !RouteStops.Any())
                return "Unknown Route";

            // Order by sequence to guarantee we get the true first and last stops
            var orderedStops = RouteStops.OrderBy(rs => rs.StopSequence).ToList();

            var firstStop = orderedStops.First().Stop?.StopName ?? "Unknown Origin";
            var lastStop = orderedStops.Last().Stop?.StopName ?? "Unknown Destination";

            if (direction == RouteDirection.Inbound)
            {
                return $"{lastStop} -> {firstStop}";
            }

            return $"{firstStop} -> {lastStop}";
        }
    }
}