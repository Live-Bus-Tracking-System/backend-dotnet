using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Events.Organisations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace BusTracker.Application.Features.Organisations.Commands.ConfirmOrgDeletion
{
    public class ConfirmOrgDeletionCommandHandler : IRequestHandler<ConfirmOrgDeletionCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IOrgDeletionIntentCache _intentCache;
        private readonly IValidator<ConfirmOrgDeletionCommand> _validator;

        public ConfirmOrgDeletionCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IOrgDeletionIntentCache intentCache,
            IValidator<ConfirmOrgDeletionCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _intentCache = intentCache;
            _validator = validator;
        }

        public async Task Handle(ConfirmOrgDeletionCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (_currentUser.UserId == null)
                throw new UnauthorizedException("User not authenticated.");

            var intent = await _intentCache.GetConfirmIntentAsync(request.ConfirmToken);
            if (intent == null)
            {
                throw new BadRequestException("Confirmation token expired or invalid.");
            }

            if (intent.OrgId != request.OrganisationId || intent.UserId != _currentUser.UserId)
            {
                throw new ForbiddenException("Invalid request parameters.");
            }

            var org = await _db.Organizations
                .FirstOrDefaultAsync(o => o.Id == request.OrganisationId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", request.OrganisationId);

            await _intentCache.RemoveConfirmIntentAsync(request.ConfirmToken);

            org.AddDomainEvent(new OrganisationDeletedDomainEvent(org.Id, org.NormalizedEmail, org.Name));

            _db.Organizations.Remove(org);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
