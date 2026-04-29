using FluentValidation;

namespace BusTracker.Application.Features.Routes.Commands.CreateRoute
{
    public class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
    {
        public CreateRouteCommandValidator()
        {
            RuleFor(x => x.RouteNumber)
                .NotEmpty()
                .MaximumLength(50);
            
            // Allow stops to be empty, but if provided, validate elements
            RuleForEach(x => x.Stops).ChildRules(stops =>
            {
                stops.RuleFor(s => s.StopId).NotEmpty();
                stops.RuleFor(s => s.Sequence).GreaterThan(0);
            });
        }
    }
}
