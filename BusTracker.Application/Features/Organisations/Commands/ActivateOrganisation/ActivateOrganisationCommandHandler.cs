using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Organisations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Organisations.Commands.ActivateOrganisation
{
    public class ActivateOrganisationCommandHandler : IRequestHandler<ActivateOrganisationCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<ActivateOrganisationCommand> _validator;

        public ActivateOrganisationCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<ActivateOrganisationCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(ActivateOrganisationCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            if (!_currentUser.IsSuperAdmin)
                throw new ForbiddenException("Only SuperAdmin can activate organisations.");

            var org = await _db.Organizations
                .FirstOrDefaultAsync(o => o.Id == request.OrganisationId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", request.OrganisationId);

            if (org.Status == OrganisationStatus.Active)
                throw new InvalidOperationException("Organisation is already active.");

            org.Status = OrganisationStatus.Active;

            org.AddDomainEvent(new OrganisationActivatedDomainEvent(org.Id, org.NormalizedEmail, org.Name));

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
