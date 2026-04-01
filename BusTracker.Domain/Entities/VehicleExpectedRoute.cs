using BusTracker.Domain.Common;

namespace BusTracker.Domain.Entities
{
    public class VehicleExpectedRoute : AuditableEntity
    {
        public Guid VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public Guid RouteId { get; set; }
        public Route? Route { get; set; }
    }
}