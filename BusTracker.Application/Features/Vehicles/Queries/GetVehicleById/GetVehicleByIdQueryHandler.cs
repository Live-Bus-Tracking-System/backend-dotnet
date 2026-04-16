using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Features.Vehicles.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Vehicles.Queries.GetVehicleById
{
    public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<GetVehicleByIdQuery> _validator;

        public GetVehicleByIdQueryHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<GetVehicleByIdQuery> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<VehicleDto> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var vehicle = await _db.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId && !v.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            // Enforce org-scoping for non-SuperAdmin callers
            if (!_currentUser.IsSuperAdmin && _currentUser.OrganisationId != vehicle.OrganizationId)
                throw new ForbiddenException();

            return new VehicleDto(
                vehicle.Id, vehicle.OrganizationId, vehicle.TrackerId, vehicle.LicensePlate,
                vehicle.Name, vehicle.Capacity, vehicle.IsActive,
                vehicle.CreatedAtUtc, vehicle.CreatedBy, vehicle.LastModifiedAtUtc, vehicle.LastModifiedBy);
        }
    }
}
