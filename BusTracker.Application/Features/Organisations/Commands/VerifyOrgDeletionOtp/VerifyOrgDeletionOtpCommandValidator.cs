using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Commands.VerifyOrgDeletionOtp
{
    public class VerifyOrgDeletionOtpCommandValidator : AbstractValidator<VerifyOrgDeletionOtpCommand>
    {
        public VerifyOrgDeletionOtpCommandValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty()
                .WithMessage("Organisation ID is required.");

            RuleFor(x => x.IntentId)
                .NotEmpty()
                .WithMessage("Intent ID cannot be empty.");

            RuleFor(x => x.Otp)
                .NotEmpty()
                .WithMessage("OTP code is required.")
                .Length(6)
                .WithMessage("OTP must be exactly 6 digits.");
        }
    }
}
