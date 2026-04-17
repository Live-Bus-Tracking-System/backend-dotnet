using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Vehicles.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Vehicles.Queries.GetAllVehicles
{
    public record GetAllVehiclesQuery(
        Guid? OrganisationId = null,
        bool IncludeInactive = false,
        int Page = 1,
        int PageSize = 25
    ) : IRequest<PagedResult<VehicleSummaryDto>>;
}
