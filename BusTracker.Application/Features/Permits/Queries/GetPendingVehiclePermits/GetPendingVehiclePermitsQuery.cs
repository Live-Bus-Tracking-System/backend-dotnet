using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Permits.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Permits.Queries.GetPendingVehiclePermits
{
    public record GetPendingVehiclePermitsQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<PendingPermitDto>>;
}
