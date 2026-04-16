using FluentValidation;

namespace BusTracker.Application.Features.Vehicles.Commands.UpdateVehicle
{
    public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
    {
        public UpdateVehicleCommandValidator()
        {
            // ── Global guard — must supply at least one field ──────────────────
            RuleFor(x => x)
                .Must(HaveAtLeastOneField)
                .WithName("Request")
                .WithMessage("No update fields were provided. Supply at least one field to update.");

            RuleFor(x => x.VehicleId)
                .NotEmpty().WithMessage("Vehicle ID is required.");

            // ── Group 1: Core fields (all individually optional) ───────────────
            RuleFor(x => x.LicensePlate)
                .NotEmpty().WithMessage("License plate cannot be an empty string.")
                .Length(7, 15).WithMessage("License plate must be between 7 and 15 characters.")
                .Matches("^[A-Z0-9]+$").WithMessage("License plate must contain only letters (A-Z) and numbers (0-9).")
                .When(x => x.LicensePlate is not null);

            RuleFor(x => x.TrackerId)
                .NotEmpty().WithMessage("Tracker ID cannot be an empty string.")
                .MaximumLength(100).WithMessage("Tracker ID must not exceed 100 characters.")
                .When(x => x.TrackerId is not null);

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Vehicle name must not exceed 100 characters.")
                .When(x => x.Name is not null);

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.")
                .LessThanOrEqualTo(200).WithMessage("Capacity must not exceed 200.")
                .When(x => x.Capacity is not null);

            // ── Group 2: Registration cert renewal ────────────────────────────
            RuleFor(x => x.RegistrationCertificateUrl)
                .Must(BeValidHttpsUrl)
                .WithMessage("Registration certificate URL must be a valid HTTPS URL.")
                .When(x => x.RegistrationCertificateUrl is not null);

            RuleFor(x => x.RegistrationCertificateNumber)
                .MaximumLength(50)
                .When(x => x.RegistrationCertificateNumber is not null);

            RuleFor(x => x.RegistrationCertExpiresAt)
                .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Registration certificate URL was provided but the expiry date is in the past.")
                .When(x => x.RegistrationCertificateUrl is not null && x.RegistrationCertExpiresAt is not null);

            // ── Group 3: Permit cert renewal ──────────────────────────────────
            RuleFor(x => x.PermitCertificateUrl)
                .Must(BeValidHttpsUrl)
                .WithMessage("Permit certificate URL must be a valid HTTPS URL.")
                .When(x => x.PermitCertificateUrl is not null);

            RuleFor(x => x.PermitCertificateNumber)
                .MaximumLength(50)
                .When(x => x.PermitCertificateNumber is not null);

            RuleFor(x => x.PermitCertExpiresAt)
                .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Permit certificate URL was provided but the expiry date is in the past.")
                .When(x => x.PermitCertificateUrl is not null && x.PermitCertExpiresAt is not null);

            // ── Group 4: Registration notes ───────────────────────────────────
            RuleFor(x => x.IntendedRouteName)
                .MaximumLength(100).When(x => x.IntendedRouteName is not null);

            RuleFor(x => x.StartStopName)
                .MaximumLength(100).When(x => x.StartStopName is not null);

            RuleFor(x => x.EndStopName)
                .MaximumLength(100).When(x => x.EndStopName is not null);

            RuleFor(x => x.AdditionalNotes)
                .MaximumLength(500).When(x => x.AdditionalNotes is not null);

            // ── Group 5: Permit admin ─────────────────────────────────────────
            RuleFor(x => x.PermitNumber)
                .MaximumLength(50).When(x => x.PermitNumber is not null);
        }

        private static bool HaveAtLeastOneField(UpdateVehicleCommand cmd) =>
            cmd.LicensePlate is not null   || cmd.TrackerId is not null           ||
            cmd.Name is not null           || cmd.Capacity is not null            ||
            cmd.RegistrationCertificateUrl is not null                            ||
            cmd.PermitCertificateUrl is not null                                  ||
            cmd.IntendedRouteName is not null || cmd.StartStopName is not null    ||
            cmd.EndStopName is not null    || cmd.AdditionalNotes is not null     ||
            cmd.PermitNumber is not null;

        private static bool BeValidHttpsUrl(string? url) =>
            url is not null &&
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps;
    }
}
