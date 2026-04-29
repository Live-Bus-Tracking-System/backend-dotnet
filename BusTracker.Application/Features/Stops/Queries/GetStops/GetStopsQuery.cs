using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Features.Stops.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Stops.Queries.GetStops
{
    public record GetStopsQuery() : IRequest<List<StopDto>>;

    public class GetStopsQueryHandler : IRequestHandler<GetStopsQuery, List<StopDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetStopsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<List<StopDto>> Handle(GetStopsQuery request, CancellationToken cancellationToken)
        {
            var query = _db.Stops
                .Where(s => !s.IsDeleted)
                .AsNoTracking();

            // SuperAdmin sees all stops.
            // OrgAdmin sees their own org's stops + all global stops.
            if (!_currentUser.IsSuperAdmin)
            {
                query = query.Where(s => s.IsGlobal || s.OrganizationId == _currentUser.OrganisationId);
            }

            return await query.Select(s => new StopDto
            {
                Id = s.Id,
                OrganizationId = s.OrganizationId,
                StopName = s.StopName,
                Latitude = s.Location.Y,
                Longitude = s.Location.X,
                IsGlobal = s.IsGlobal,
                DataOrigin = s.DataOrigin
            }).ToListAsync(cancellationToken);
        }
    }
}
