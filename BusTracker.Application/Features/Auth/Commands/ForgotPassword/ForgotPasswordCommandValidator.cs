using BusTracker.Application.Common.Interfaces;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace BusTracker.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        private readonly IPhoneNumberService _phoneNumberService;
        private static readonly EmailAddressAttribute EmailValidator = new();
        public ForgotPasswordCommandValidator(IPhoneNumberService phoneNumberService)
        {
            _phoneNumberService = phoneNumberService;

            RuleFor(v => v.EmailOrPhone)
                .NotEmpty().WithMessage("Email or Phone Number is required")
                .Must(BeValidEmailOrPhone).WithMessage("Please provide a valid email address or phone number");
        }

        private bool BeValidEmailOrPhone(string emailOrPhone)
        {
            if (string.IsNullOrWhiteSpace(emailOrPhone))
                return false;

            emailOrPhone = emailOrPhone.Trim();

            return IsValidEmail(emailOrPhone) || _phoneNumberService.IsValid(emailOrPhone);
        }

        private bool IsValidEmail(string email)
        {
            return EmailValidator.IsValid(email);
        }
    }
}
