using FluentValidation;

namespace BusTracker.Application.Features.Stops.Commands.CreateStop
{
    public class CreateStopCommandValidator : AbstractValidator<CreateStopCommand>
    {
        public CreateStopCommandValidator()
        {
            RuleFor(x => x.StopName)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180);
        }
    }
}
