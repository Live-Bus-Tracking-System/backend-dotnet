using BusTracker.Application.Common.Auth;
using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Features.Organisations.DTOs;
using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Organisations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Organisations.Commands.CreateOrganisation
{
    public class CreateOrganisationCommandHandler : IRequestHandler<CreateOrganisationCommand, OrganisationDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<CreateOrganisationCommand> _validator;
        private readonly IPhoneNumberService _phoneNumberService;
        private readonly IIdentityService _identityService;

        public CreateOrganisationCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<CreateOrganisationCommand> validator,
            IPhoneNumberService phoneNumberService,
            IIdentityService identityService)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
            _phoneNumberService = phoneNumberService;
            _identityService = identityService;
        }

        public async Task<OrganisationDto> Handle(CreateOrganisationCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedPhoneNumber = _phoneNumberService.Normalize(request.PhoneNumber);

            var existingFields = await _db.Organizations
                .AsNoTracking()
                .Where(o => !o.IsDeleted && (o.NormalizedEmail == normalizedEmail || o.NormalizedPhoneNumber == normalizedPhoneNumber))
                .Select(o => new { o.NormalizedEmail, o.NormalizedPhoneNumber })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingFields != null)
            {
                var errors = new List<FluentValidation.Results.ValidationFailure>(2);

                if (existingFields.NormalizedEmail == normalizedEmail)
                    errors.Add(new FluentValidation.Results.ValidationFailure("Email", "An organisation with this email already exists."));

                if (existingFields.NormalizedPhoneNumber == normalizedPhoneNumber)
                    errors.Add(new FluentValidation.Results.ValidationFailure("PhoneNumber", "An organisation with this phone number already exists."));

                throw new CustomValidationException(errors);
            }
            //========================================================
            // SuperAdmin creates orgs as immediately Active.
            // Any other caller (self-registration) lands in PendingVerification.
            //var status = _currentUser.IsSuperAdmin
            //    ? OrganisationStatus.Active
            //    : OrganisationStatus.PendingVerification;
            //======================================================== this feature is disabled for the mvp for now allow user to diectly create active organisation
            var status = OrganisationStatus.Active;

            var org = new Organization
            {
                Name = request.Name.Trim(),
                NormalizedEmail = normalizedEmail,
                NormalizedPhoneNumber = request.PhoneNumber.Trim(),
                Type = request.Type,
                Status = status,
            };

            org.AddDomainEvent(new OrganisationCreatedDomainEvent(org.Id, org.Name, org.NormalizedEmail, org.NormalizedPhoneNumber, _currentUser.UserId!));

            _db.Organizations.Add(org);
            await _db.SaveChangesAsync(cancellationToken);

            // Assign the user who created it as the OrgAdmin and bind the OrganisationId
            await _identityService.AssignUserToOrganisationAsync(_currentUser.UserId!, org.Id, Roles.OrgAdmin);

            return MapToDto(org);
        }

        private static OrganisationDto MapToDto(Organization org)
        {
            return new OrganisationDto(
                Id: org.Id,
                Name: org.Name,
                NormalizedEmail: org.NormalizedEmail,
                NormalizedPhoneNumber: org.NormalizedPhoneNumber,
                Type: org.Type,
                Status: org.Status,
                IsOperational: org.IsOperational,
                CreatedAtUtc: org.CreatedAtUtc,
                CreatedBy: org.CreatedBy,
                LastModifiedAtUtc: org.LastModifiedAtUtc,
                LastModifiedBy: org.LastModifiedBy
            );
        }
    }
}
