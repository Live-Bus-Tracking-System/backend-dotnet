using BusTracker.Application.Features.Stops.DTOs;
using BusTracker.Domain.Enums;

namespace BusTracker.Application.Features.Routes.DTOs
{
    public class RouteStopDto
    {
        public Guid StopId { get; set; }
        public int Sequence { get; set; }
        public double? DistanceToNextStopMeters { get; set; }
        public string? SegmentPolyline { get; set; }
        public StopDto? StopDetails { get; set; }
    }

    public class RouteDto
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string RouteNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DataOrigin DataOrigin { get; set; }
        public string? FullPolyline { get; set; }
        public List<RouteStopDto> Stops { get; set; } = new();
    }
    public class RouteSearchResultDto
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string RouteNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public int RelevanceScore { get; set; }
    }
}
