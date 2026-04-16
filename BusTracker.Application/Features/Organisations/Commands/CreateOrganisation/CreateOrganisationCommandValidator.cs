using BusTracker.Application.Common.Interfaces;
using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.CreateOrganisation
{
    public class CreateOrganisationCommandValidator : AbstractValidator<CreateOrganisationCommand>
    {
        private readonly IPhoneNumberService _phoneNumberService;
        public CreateOrganisationCommandValidator(IPhoneNumberService phoneNumberService)
        {

            _phoneNumberService = phoneNumberService;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Organisation name is required.")
                .MaximumLength(200).WithMessage("Organisation name must not exceed 200 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email address is required.")
                .EmailAddress().WithMessage("Please enter a valid email address.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

            RuleFor(v => v.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(p => _phoneNumberService.IsValid(p))
                .WithMessage("Please provide a valid phone number.");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid organisation type selected.");
        }
    }
}