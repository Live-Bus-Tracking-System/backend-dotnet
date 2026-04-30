using MediatR;

namespace BusTracker.Application.Features.Routes.Commands.CreateRoute
{
    public class RouteStopInputDto
    {
        public Guid? StopId { get; set; }
        public string StopName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Sequence { get; set; }
        public string? SegmentPolyline { get; set; }
        public double? Distance { get; set; }
    }

    public record CreateRouteCommand(
        string RouteNumber,
        string? FullPolyline,
        bool IsPublic,
        List<RouteStopInputDto> Stops
    ) : IRequest<Guid>;
}
