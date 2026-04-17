using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Enums;
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


            var docs = await _db.ComplianceDocuments
                .AsNoTracking()
                .Where(d => d.EntityId == vehicle.Id
                         && d.EntityType == ComplianceDocumentEntityType.Vehicle
                         && !d.IsDeleted)
                .Select(d => new { d.DocumentType, d.IsVerified })
                .ToListAsync(cancellationToken);

            var regCert = docs.FirstOrDefault(d => d.DocumentType == ComplianceDocumentType.VehicleRegistration);
            var permitCert = docs.FirstOrDefault(d => d.DocumentType == ComplianceDocumentType.RoutePermitDoc);

            var failures = new List<FluentValidation.Results.ValidationFailure>(2);

            if (regCert is null || !regCert.IsVerified)
                failures.Add(new("VehicleRegistrationCert",
                    "Vehicle registration certificate has not been verified. Verification is required before activation."));

            if (permitCert is null || !permitCert.IsVerified)
                failures.Add(new("PermitCert",
                    "Route permit certificate has not been verified. Verification is required before activation."));

            if (failures.Count > 0)
                throw new CustomValidationException(failures);

            if (vehicle.IsActive == false)
            {
                vehicle.IsActive = true;

                vehicle.AddDomainEvent(new VehicleActivatedDomainEvent(vehicle.Id, vehicle.LicensePlate));
            }

            vehicle.IsActive = true;

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
