using BusTracker.Application.Features.Vehicles.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Vehicles.Queries.GetAllVehicles
{
    public record GetAllVehiclesQuery(
        Guid? OrganisationId = null,
        bool IncludeInactive = false
    ) : IRequest<IEnumerable<VehicleSummaryDto>>;
}
