using MediatR;

namespace BusTracker.Application.Features.Vehicles.Commands.ActivateVehicle
{
    public record ActivateVehicleCommand(Guid VehicleId) : IRequest;
}
