using FluentValidation;

namespace BusTracker.Application.Features.Vehicles.Commands.RegisterVehicle
{
    public class RegisterVehicleCommandValidator : AbstractValidator<RegisterVehicleCommand>
    {
        public RegisterVehicleCommandValidator()
        {
            RuleFor(x => x.LicensePlate)
                .NotEmpty().WithMessage("License plate cannot be an empty string.")
                .Length(7, 15).WithMessage("License plate must be between 7 and 15 characters.")
                .Matches("^[A-Z0-9]+$").WithMessage("License plate must contain only letters (A-Z) and numbers (0-9).");

            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage("Tracker ID is required.")
                .MaximumLength(100).WithMessage("Tracker ID must not exceed 100 characters.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Vehicle name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Vehicle name cannot be empty or whitespace.")
                .MaximumLength(100)
                .WithMessage("Vehicle name must not exceed 100 characters.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.")
                .LessThanOrEqualTo(200).WithMessage("Capacity must not exceed 200.")
                .When(x => x.Capacity is not null);

            RuleFor(x => x.RegistrationCertificateObjectKey)
                .NotEmpty().WithMessage("Vehicle registration certificate object key is required.")
                .Must(BeValidObjectKey).WithMessage("Registration certificate object key format is invalid.");

            RuleFor(x => x.PermitCertificateObjectKey)
                .NotEmpty().WithMessage("Permit certificate object key is required.")
                .Must(BeValidObjectKey).WithMessage("Permit certificate object key format is invalid.");

            RuleFor(x => x.RegistrationCertificateNumber)
                .MaximumLength(50).WithMessage("")
                .When(x => x.RegistrationCertificateNumber is not null);

            RuleFor(x => x.PermitCertificateNumber)
                .MaximumLength(50).When(x => x.PermitCertificateNumber is not null);

            // Expiry must be in the future if supplied
            RuleFor(x => x.RegistrationCertExpiresAt)
                .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Registration certificate appears to be expired.")
                .When(x => x.RegistrationCertExpiresAt is not null);

            RuleFor(x => x.PermitCertExpiresAt)
                .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Permit certificate appears to be expired.")
                .When(x => x.PermitCertExpiresAt is not null);

            RuleFor(x => x.AdditionalNotes)
                .MaximumLength(500).When(x => x.AdditionalNotes is not null);

            RuleFor(x => x.IntendedRouteName)
                .MaximumLength(100).When(x => x.IntendedRouteName is not null);
        }

        private static bool BeValidObjectKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            
            // Basic path traversal / malformed key prevention
            return !key.Contains("..") && 
                   !key.Contains("\\") && 
                   !key.StartsWith("/");
        }
    }
}
