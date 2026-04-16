using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Events.Vehicles;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Vehicles.Commands.ActivateVehicle
{
    public class ActivateVehicleCommandHandler : IRequestHandler<ActivateVehicleCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<ActivateVehicleCommand> _validator;

        public ActivateVehicleCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<ActivateVehicleCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(ActivateVehicleCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId && !v.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            if (!_currentUser.IsSuperAdmin && _currentUser.OrganisationId != vehicle.OrganizationId)
                throw new ForbiddenException();

            vehicle.IsActive = true;

            vehicle.AddDomainEvent(new VehicleActivatedDomainEvent(vehicle.Id, vehicle.LicensePlate));

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
