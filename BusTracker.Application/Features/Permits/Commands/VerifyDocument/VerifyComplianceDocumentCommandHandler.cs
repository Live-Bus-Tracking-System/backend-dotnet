using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Permits.Commands.VerifyDocument
{
    public class VerifyComplianceDocumentCommandHandler : IRequestHandler<VerifyComplianceDocumentCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<VerifyComplianceDocumentCommand> _validator;

        public VerifyComplianceDocumentCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<VerifyComplianceDocumentCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(VerifyComplianceDocumentCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            // Fetch the permit to ensure it exists and is pending
            var permit = await _db.VehiclePermits
                .FirstOrDefaultAsync(p => p.Id == request.PermitId && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("VehiclePermit", request.PermitId);

            if (permit.PermitStatus != PermitStatus.Pending)
                throw new CustomValidationException([new("PermitId", "Only pending permits can have their documents verified.")]);

            // Fetch the document
            var document = await _db.ComplianceDocuments
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == permit.VehicleId && !d.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("ComplianceDocument", request.DocumentId);

            document.IsVerified = request.IsVerified;
            document.VerifiedBy = _currentUser.UserId;
            document.VerifiedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
