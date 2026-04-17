using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Vehicles.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Vehicles.Queries.GetAllVehicles
{
    public class GetAllVehiclesQueryHandler : IRequestHandler<GetAllVehiclesQuery, PagedResult<VehicleSummaryDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<GetAllVehiclesQuery> _validator;

        public GetAllVehiclesQueryHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<GetAllVehiclesQuery> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<PagedResult<VehicleSummaryDto>> Handle(GetAllVehiclesQuery request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var query = _db.Vehicles.AsNoTracking().Where(v => !v.IsDeleted);

            if (_currentUser.IsSuperAdmin)
            {
                // SuperAdmin can optionally filter by a specific org
                if (request.OrganisationId is not null)
                    query = query.Where(v => v.OrganizationId == request.OrganisationId);
            }
            else
            {
                // Non-SuperAdmin callers are always scoped to their own organisation
                if (_currentUser.OrganisationId is null)
                    throw new ForbiddenException();

                query = query.Where(v => v.OrganizationId == _currentUser.OrganisationId);
            }

            if (!request.IncludeInactive)
                query = query.Where(v => v.IsActive);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(v => v.LicensePlate)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(v => new VehicleSummaryDto(
                    v.Id,
                    v.OrganizationId,
                    v.LicensePlate,
                    v.Name,
                    v.Capacity,
                    v.IsActive,
                    v.CreatedAtUtc))
                .ToListAsync(cancellationToken);

            return PagedResult<VehicleSummaryDto>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }
}
