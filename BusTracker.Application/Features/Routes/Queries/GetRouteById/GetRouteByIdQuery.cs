using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Features.Routes.DTOs;
using BusTracker.Application.Features.Stops.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Routes.Queries.GetRouteById
{
    public record GetRouteByIdQuery(Guid Id) : IRequest<RouteDto>;

    public class GetRouteByIdQueryHandler : IRequestHandler<GetRouteByIdQuery, RouteDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetRouteByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<RouteDto> Handle(GetRouteByIdQuery request, CancellationToken cancellationToken)
        {
            var route = await _db.Routes
                .Include(r => r.RouteStops)
                    .ThenInclude(rs => rs.Stop)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Route", request.Id);

            // Access control logic
            if (!_currentUser.IsSuperAdmin)
            {
                // If it is a Public Transit user, they theoretically can read routes assigned to their vehicles or public routes.
                // Assuming for now, OrgAdmins can only see routes belonging to their Org OR IsPublic routes.
                if (!route.IsPublic && route.OrganizationId != _currentUser.OrganisationId)
                {
                    throw new ForbiddenException("You do not have permission to view this private route.");
                }
            }

            var dto = new RouteDto
            {
                Id = route.Id,
                OrganizationId = route.OrganizationId,
                RouteNumber = route.RouteNumber ?? "",
                RouteName = route.GetRouteName(), // Dynamic calculation based on sequenced stops
                IsPublic = route.IsPublic,
                DataOrigin = route.DataOrigin,
                FullPolyline = route.RouteShapeCoordinates,
                Stops = route.RouteStops
                    .Where(rs => rs.Stop != null && !rs.Stop.IsDeleted) // Filter out deleted stops
                    .OrderBy(rs => rs.StopSequence)
                    .Select(rs => new RouteStopDto
                    {
                        StopId = rs.StopId,
                        Sequence = rs.StopSequence,
                        DistanceToNextStopMeters = rs.DistanceToNextStopMeters,
                        SegmentPolyline = rs.PathToNextStop,
                        StopDetails = new StopDto
                        {
                            Id = rs.Stop!.Id,
                            OrganizationId = rs.Stop.OrganizationId,
                            StopName = rs.Stop.StopName,
                            Latitude = rs.Stop.Location.Y,
                            Longitude = rs.Stop.Location.X,
                            IsGlobal = rs.Stop.IsGlobal,
                            DataOrigin = rs.Stop.DataOrigin
                        }
                    }).ToList()
            };

            return dto;
        }
    }
}
