using FluentValidation;

namespace BusTracker.Application.Features.Vehicles.Commands.DeactivateVehicle
{
    public class DeactivateVehicleCommandValidator : AbstractValidator<DeactivateVehicleCommand>
    {
        public DeactivateVehicleCommandValidator()
        {
            RuleFor(x => x.VehicleId)
                .NotEmpty().WithMessage("Vehicle ID is required.");
        }
    }
}
