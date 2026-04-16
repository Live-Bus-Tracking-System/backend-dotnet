using MediatR;

namespace BusTracker.Application.Features.Vehicles.Commands.DeleteVehicle
{
    public record DeleteVehicleCommand(Guid VehicleId) : IRequest;
}
