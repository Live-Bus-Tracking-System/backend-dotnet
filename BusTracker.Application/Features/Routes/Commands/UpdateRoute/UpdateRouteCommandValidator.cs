using FluentValidation;

namespace BusTracker.Application.Features.Routes.Commands.UpdateRoute
{
    public class UpdateRouteCommandValidator : AbstractValidator<UpdateRouteCommand>
    {
        public UpdateRouteCommandValidator()
        {
            RuleFor(x => x.RouteId).NotEmpty();

            RuleFor(x => x.RouteNumber)
                .NotEmpty()
                .MaximumLength(50);
            
            RuleForEach(x => x.Stops).ChildRules(stops =>
            {
                stops.RuleFor(s => s.StopName)
                    .NotEmpty()
                    .When(s => s.StopId == null)
                    .WithMessage("StopName is required if StopId is not provided.");

                stops.RuleFor(s => s.Latitude)
                    .InclusiveBetween(-90, 90)
                    .When(s => s.StopId == null);

                stops.RuleFor(s => s.Longitude)
                    .InclusiveBetween(-180, 180)
                    .When(s => s.StopId == null);

                stops.RuleFor(s => s.Sequence).GreaterThan(0);
            });
        }
    }
}
