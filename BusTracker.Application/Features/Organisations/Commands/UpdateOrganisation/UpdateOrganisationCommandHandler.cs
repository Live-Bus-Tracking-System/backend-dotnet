using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Features.Organisations.DTOs;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Organisations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Organisations.Commands.UpdateOrganisation
{
    public class UpdateOrganisationCommandHandler : IRequestHandler<UpdateOrganisationCommand, OrganisationDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<UpdateOrganisationCommand> _validator;

        public UpdateOrganisationCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<UpdateOrganisationCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<OrganisationDto> Handle(UpdateOrganisationCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var org = await _db.Organizations
                .FirstOrDefaultAsync(o => o.Id == request.OrganisationId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", request.OrganisationId);

            // Non-SuperAdmin callers can only update their own organisation
            if (!_currentUser.IsSuperAdmin && _currentUser.OrganisationId != org.Id)
                throw new ForbiddenException();

            if (!string.IsNullOrWhiteSpace(request?.Name))
                org.Name = request.Name.Trim();

            if (!string.IsNullOrWhiteSpace(request?.Email))
                org.NormalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(request?.PhoneNumber))
                org.NormalizedPhoneNumber = request.PhoneNumber.Trim();


            org.AddDomainEvent(new OrganisationUpdatedDomainEvent(org.Id, org.NormalizedEmail, org.Name));

            await _db.SaveChangesAsync(cancellationToken);

            return new OrganisationDto(
                org.Id,
                org.Name,
                org.NormalizedEmail,
                org.NormalizedPhoneNumber,
                org.Type,
                org.Status,
                org.IsOperational,
                org.CreatedAtUtc,
                org.CreatedBy,
                org.LastModifiedAtUtc,
                org.LastModifiedBy);
        }
    }
}
