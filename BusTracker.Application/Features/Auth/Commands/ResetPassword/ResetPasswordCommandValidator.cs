using BusTracker.Application.Common.Interfaces;
using FluentValidation;
using NetTopologySuite.Densify;
using System.ComponentModel.DataAnnotations;

namespace BusTracker.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        private readonly IPhoneNumberService _phoneNumberService;
        private static readonly EmailAddressAttribute EmailValidator = new();

        public ResetPasswordCommandValidator(IPhoneNumberService phoneNumberService)
        {
            _phoneNumberService = phoneNumberService;

            RuleFor(v => v.EmailOrPhone)
                .NotEmpty().WithMessage("Email or Phone Number is required")
                .Must(BeValidEmailOrPhone).WithMessage("Please provide a valid email address or phone number");

            RuleFor(v => v.Token).NotEmpty().WithMessage("Reset token is required.");

            RuleFor(v => v.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
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