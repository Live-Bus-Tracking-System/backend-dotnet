using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Events.Vehicles;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Vehicles.Commands.DeactivateVehicle
{
    public class DeactivateVehicleCommandHandler : IRequestHandler<DeactivateVehicleCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IVehicleStateCache _cache;
        private readonly IValidator<DeactivateVehicleCommand> _validator;

        public DeactivateVehicleCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IVehicleStateCache cache,
            IValidator<DeactivateVehicleCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _cache = cache;
            _validator = validator;
        }

        public async Task Handle(DeactivateVehicleCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId && !v.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            if (!_currentUser.IsSuperAdmin && _currentUser.OrganisationId != vehicle.OrganizationId)
                throw new ForbiddenException();

            if (!vehicle.IsActive)
                return;

            vehicle.IsActive = false;
            vehicle.AddDomainEvent(new VehicleDeactivatedDomainEvent(vehicle.Id, vehicle.LicensePlate));

            await _db.SaveChangesAsync(cancellationToken);

            await _cache.DeleteTrackerStateAsync(vehicle.TrackerId);
        }
    }
}
