using FluentValidation;

namespace BusTracker.Application.Features.Permits.Commands.ReviewPermit
{
    public class ReviewVehiclePermitCommandValidator : AbstractValidator<ReviewVehiclePermitCommand>
    {
        public ReviewVehiclePermitCommandValidator()
        {
            RuleFor(x => x.PermitId).NotEmpty();

            RuleFor(x => x.RejectionReason)
                .NotEmpty().WithMessage("Rejection reason must be provided if the permit is rejected.")
                .When(x => !x.IsApproved);

            RuleFor(x => x.RouteId)
                .NotEmpty().WithMessage("A Route ID must be provided to assign the vehicle upon approval.")
                .When(x => x.IsApproved);
        }
    }
}
