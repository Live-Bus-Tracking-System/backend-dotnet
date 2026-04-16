using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Organisations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Organisations.Commands.SuspendOrganisation
{
    public class SuspendOrganisationCommandHandler : IRequestHandler<SuspendOrganisationCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<SuspendOrganisationCommand> _validator;

        public SuspendOrganisationCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<SuspendOrganisationCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(SuspendOrganisationCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (!_currentUser.IsSuperAdmin)
                throw new ForbiddenException("Only SuperAdmin can suspend organisations.");

            var org = await _db.Organizations
                .FirstOrDefaultAsync(o => o.Id == request.OrganisationId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", request.OrganisationId);

            if (org.Status == OrganisationStatus.Suspended)
                throw new InvalidOperationException("Organisation is already suspended.");

            org.Status = OrganisationStatus.Suspended;

            org.AddDomainEvent(new OrganisationSuspendedDomainEvent(org.Id, org.NormalizedEmail, org.Name, request.Reason));

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
