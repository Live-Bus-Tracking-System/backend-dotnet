using BusTracker.Application.Features.Vehicles.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Vehicles.Queries.GetVehicleById
{
    public record GetVehicleByIdQuery(Guid VehicleId) : IRequest<VehicleDto>;
}
