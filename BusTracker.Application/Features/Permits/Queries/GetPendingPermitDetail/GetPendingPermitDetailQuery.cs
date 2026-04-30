using BusTracker.Application.Features.Permits.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Permits.Queries.GetPendingPermitDetail
{
    public record GetPendingPermitDetailQuery(Guid PermitId) : IRequest<PendingPermitDetailDto>;
}
