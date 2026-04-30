using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Permits.DTOs;
using BusTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Permits.Queries.GetPendingVehiclePermits
{
    public class GetPendingVehiclePermitsQueryHandler : IRequestHandler<GetPendingVehiclePermitsQuery, PagedResult<PendingPermitDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetPendingVehiclePermitsQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<PendingPermitDto>> Handle(GetPendingVehiclePermitsQuery request, CancellationToken cancellationToken)
        {
            var query = _db.VehiclePermits
                .AsNoTracking()
                .Include(p => p.Vehicle)
                .Include(p => p.Organization)
                .Where(p => !p.IsDeleted && p.PermitStatus == PermitStatus.Pending);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(p => p.CreatedAtUtc)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PendingPermitDto(
                    p.Id,
                    p.VehicleId,
                    p.OrganizationId,
                    p.Organization!.Name,
                    p.Vehicle!.LicensePlate,
                    p.Vehicle.Name,
                    p.CreatedAtUtc
                ))
                .ToListAsync(cancellationToken);

            return PagedResult<PendingPermitDto>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }
}
