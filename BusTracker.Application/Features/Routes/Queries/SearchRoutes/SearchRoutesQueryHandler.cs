using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Helpers;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Routes.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Routes.Queries.SearchRoutes
{
    public class SearchRoutesQueryHandler : IRequestHandler<SearchRoutesQuery, PagedResult<RouteSearchResultDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<SearchRoutesQuery> _validator;

        public SearchRoutesQueryHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<SearchRoutesQuery> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<PagedResult<RouteSearchResultDto>> Handle(SearchRoutesQuery request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var query = _db.Routes
                .Include(r => r.RouteStops)
                    .ThenInclude(rs => rs.Stop)
                .AsNoTracking()
                .Where(r => !r.IsDeleted);

            // Access Control
            if (!_currentUser.IsSuperAdmin)
            {
                if (_currentUser.OrganisationId.HasValue)
                {
                    // Organization users can see their own routes + public routes
                    query = query.Where(r => r.OrganizationId == _currentUser.OrganisationId || r.IsPublic);
                }
                else
                {
                    // Unauthenticated/Passengers can only see public routes
                    query = query.Where(r => r.IsPublic);
                }
            }

            // Stage 1: Broad Database Net
            // Fetch any route where RouteNumber contains the term, OR any StopName contains the term.
            // EF Core translates .Contains() to LIKE '%term%'
            var searchTermLower = request.SearchTerm.ToLower();
            query = query.Where(r => 
                (r.RouteNumber != null && r.RouteNumber.ToLower().Contains(searchTermLower)) ||
                r.RouteStops.Any(rs => rs.Stop != null && rs.Stop.StopName.ToLower().Contains(searchTermLower))
            );

            // Fetch the narrowed dataset into memory
            var dbRoutes = await query.ToListAsync(cancellationToken);

            // Stage 2: In-Memory Fuzzy Scoring
            var scoredRoutes = new List<RouteSearchResultDto>();

            foreach (var route in dbRoutes)
            {
                var routeName = route.GetRouteName();
                var routeNumber = route.RouteNumber ?? string.Empty;

                // Calculate Relevance
                int score = 0;

                // Exact match gets highest score
                if (routeNumber.Equals(request.SearchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    score = 100;
                }
                else if (routeName.Equals(request.SearchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    score = 90;
                }
                else
                {
                    // Fuzzy match on RouteNumber
                    int numberScore = StringMetrics.CalculateFuzzyScore(request.SearchTerm, routeNumber);
                    
                    // Fuzzy match on RouteName
                    int nameScore = StringMetrics.CalculateFuzzyScore(request.SearchTerm, routeName);

                    // Stop keyword matching
                    int stopScore = 0;
                    if (route.RouteStops.Any(rs => rs.Stop != null && rs.Stop.StopName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)))
                    {
                        stopScore = 50; // Partial keyword hit on an intermediate stop
                    }

                    // Take the best score
                    score = Math.Max(Math.Max(numberScore, nameScore), stopScore);
                }

                scoredRoutes.Add(new RouteSearchResultDto
                {
                    Id = route.Id,
                    OrganizationId = route.OrganizationId,
                    RouteNumber = routeNumber,
                    RouteName = routeName,
                    IsPublic = route.IsPublic,
                    RelevanceScore = score
                });
            }

            // Stage 3: Sort by Relevance and Paginate
            var orderedRoutes = scoredRoutes
                .OrderByDescending(r => r.RelevanceScore)
                .ThenBy(r => r.RouteName)
                .ToList();

            int totalCount = orderedRoutes.Count;
            
            var pagedItems = orderedRoutes
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return PagedResult<RouteSearchResultDto>.Create(pagedItems, totalCount, request.Page, request.PageSize);
        }
    }
}
