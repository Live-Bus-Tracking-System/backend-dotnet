using MediatR;

namespace BusTracker.Application.Features.Stops.Commands.CreateStop
{
    public record CreateStopCommand(string StopName, double Latitude, double Longitude, bool IsGlobal) : IRequest<Guid>;
}
