using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Events.Organisations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Organisations.Commands.DeleteOrganisation
{
    public class DeleteOrganisationCommandHandler : IRequestHandler<DeleteOrganisationCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<DeleteOrganisationCommand> _validator;

        public DeleteOrganisationCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<DeleteOrganisationCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(DeleteOrganisationCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (!_currentUser.IsSuperAdmin)
                throw new ForbiddenException("Only SuperAdmin can use this direct deletion endpoint. Organisation Admins must use the multi-step MFA deletion flow.");

            var org = await _db.Organizations
                .FirstOrDefaultAsync(o => o.Id == request.OrganisationId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", request.OrganisationId);

            org.AddDomainEvent(new OrganisationDeletedDomainEvent(org.Id, org.NormalizedEmail, org.Name));

            _db.Organizations.Remove(org);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
