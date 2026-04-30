using BusTracker.Application.Features.Routes.Commands.CreateRoute;
using MediatR;

namespace BusTracker.Application.Features.Routes.Commands.UpdateRoute
{
    public record UpdateRouteCommand(
        Guid RouteId,
        string RouteNumber,
        string? FullPolyline,
        bool IsPublic,
        List<RouteStopInputDto> Stops
    ) : IRequest;
}
