using FluentValidation;

namespace BusTracker.Application.Features.Vehicles.Commands.ActivateVehicle
{
    public class ActivateVehicleCommandValidator : AbstractValidator<ActivateVehicleCommand>
    {
        public ActivateVehicleCommandValidator()
        {
            RuleFor(x => x.VehicleId)
                .NotEmpty().WithMessage("Vehicle ID is required.");
        }
    }
}
