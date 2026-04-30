using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Vehicles;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Permits.Commands.ReviewPermit
{
    public class ReviewVehiclePermitCommandHandler : IRequestHandler<ReviewVehiclePermitCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<ReviewVehiclePermitCommand> _validator;

        public ReviewVehiclePermitCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<ReviewVehiclePermitCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(ReviewVehiclePermitCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var permit = await _db.VehiclePermits
                .Include(p => p.Vehicle)
                .FirstOrDefaultAsync(p => p.Id == request.PermitId && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("VehiclePermit", request.PermitId);

            if (permit.PermitStatus != PermitStatus.Pending)
                throw new CustomValidationException([new("PermitId", "Only pending permits can be reviewed.")]);

            if (request.IsApproved)
            {
                // Ensure the provided RouteId actually exists
                var routeExists = await _db.Routes.AnyAsync(r => r.Id == request.RouteId && !r.IsDeleted, cancellationToken);
                if (!routeExists)
                    throw new CustomValidationException([new("RouteId", "The assigned route does not exist.")]);

                // Ensure ALL compliance documents for this vehicle are verified
                var unverifiedDocsExist = await _db.ComplianceDocuments
                    .AnyAsync(d => d.EntityId == permit.VehicleId && d.EntityType == ComplianceDocumentEntityType.Vehicle && !d.IsDeleted && !d.IsVerified, cancellationToken);

                if (unverifiedDocsExist)
                    throw new CustomValidationException([new("Documents", "All compliance documents must be verified before approving the permit.")]);

                // Apply Approval
                permit.PermitStatus = PermitStatus.Active;
                permit.ApprovedBy = _currentUser.UserId;
                permit.VerifiedAtUtc = DateTime.UtcNow;
                permit.RouteId = request.RouteId;

                permit.Vehicle!.IsActive = true;

                permit.AddDomainEvent(new VehiclePermitApprovedDomainEvent(permit.Id, permit.VehicleId, permit.OrganizationId, _currentUser.UserId!));
            }
            else
            {
                // Apply Rejection
                permit.PermitStatus = PermitStatus.Rejected;
                permit.Notes = request.RejectionReason;

                permit.AddDomainEvent(new VehiclePermitRejectedDomainEvent(permit.Id, permit.VehicleId, permit.OrganizationId, _currentUser.UserId!, request.RejectionReason!));
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
