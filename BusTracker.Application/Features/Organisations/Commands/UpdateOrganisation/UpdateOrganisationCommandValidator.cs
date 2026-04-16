using BusTracker.Application.Common.Interfaces;
using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.UpdateOrganisation
{
    public class UpdateOrganisationCommandValidator : AbstractValidator<UpdateOrganisationCommand>
    {
        private readonly IPhoneNumberService _phoneNumberService;
        public UpdateOrganisationCommandValidator(IPhoneNumberService phoneNumberService)
        {
            _phoneNumberService = phoneNumberService;

            RuleFor(x => x.OrganisationId)
                .NotEmpty()
                .WithMessage("Organisation ID is required.");

            When(x => x.Name is not null, () =>
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Organisation name cannot be empty if provided.")
                    .MaximumLength(200)
                    .WithMessage("Organisation name must not exceed 200 characters."));

            When(x => x.Email is not null, () =>
                RuleFor(x => x.Email)
                    .EmailAddress()
                    .WithMessage("Please provide a valid email address.")
                    .MaximumLength(256)
                    .WithMessage("Email address must not exceed 256 characters."));

            When(x => x.PhoneNumber is not null, () =>
                RuleFor(x => x.PhoneNumber)
                    .NotEmpty()
                    .WithMessage("Phone number cannot be empty if provided.")
                    .Must((command, p) => p is not null && _phoneNumberService.IsValid(p))
                    .WithMessage("Please provide a valid phone number."));

            // When(x => x.Type is not null, () =>
            //     RuleFor(x => x.Type)
            //         .IsInEnum()
            //         .WithMessage("Organisation type is invalid. Valid values: School, PublicTransport, Private, Government."));
        }
    }
}