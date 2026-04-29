using MediatR;

namespace BusTracker.Application.Features.Routes.Commands.CreateRoute
{
    public class RouteStopInputDto
    {
        public Guid StopId { get; set; }
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
