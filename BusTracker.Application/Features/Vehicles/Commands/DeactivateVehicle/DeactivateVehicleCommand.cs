using MediatR;

namespace BusTracker.Application.Features.Vehicles.Commands.DeactivateVehicle
{
    public record DeactivateVehicleCommand(Guid VehicleId) : IRequest;
}
