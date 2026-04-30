using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Routes.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Routes.Queries.SearchRoutes
{
    public record SearchRoutesQuery(
        string SearchTerm,
        int Page = 1,
        int PageSize = 10
    ) : IRequest<PagedResult<RouteSearchResultDto>>;
}
