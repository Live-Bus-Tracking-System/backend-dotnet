using BusTracker.Domain.Enums;

namespace BusTracker.Application.Features.Stops.DTOs
{
    public class StopDto
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string StopName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsGlobal { get; set; }
        public DataOrigin DataOrigin { get; set; }
    }
}
