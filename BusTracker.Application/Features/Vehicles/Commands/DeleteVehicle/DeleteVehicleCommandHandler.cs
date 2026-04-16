using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Events.Vehicles;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Vehicles.Commands.DeleteVehicle
{
    public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<DeleteVehicleCommand> _validator;

        public DeleteVehicleCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<DeleteVehicleCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId && !v.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            if (!_currentUser.IsSuperAdmin && _currentUser.OrganisationId != vehicle.OrganizationId)
                throw new ForbiddenException();

            vehicle.AddDomainEvent(new VehicleDeletedDomainEvent(vehicle.Id, vehicle.LicensePlate));

            _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
