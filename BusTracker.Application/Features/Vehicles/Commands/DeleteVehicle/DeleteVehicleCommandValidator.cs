using FluentValidation;

namespace BusTracker.Application.Features.Vehicles.Commands.DeleteVehicle
{
    public class DeleteVehicleCommandValidator : AbstractValidator<DeleteVehicleCommand>
    {
        public DeleteVehicleCommandValidator()
        {
            RuleFor(x => x.VehicleId)
                .NotEmpty().WithMessage("Vehicle ID is required.");
        }
    }
}
