using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Organisations.DTOs;
using BusTracker.Domain.Enums;
using MediatR;

namespace BusTracker.Application.Features.Organisations.Queries.GetAllOrganisations
{
    public record GetAllOrganisationsQuery(
        OrganisationStatus? Status = null,
        OrganizationType? Type = null,
        int Page = 1,
        int PageSize = 25
    ) : IRequest<PagedResult<OrganisationSummaryDto>>;
}
