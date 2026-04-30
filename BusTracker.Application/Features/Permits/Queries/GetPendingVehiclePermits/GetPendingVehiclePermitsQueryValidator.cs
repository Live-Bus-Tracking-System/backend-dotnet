using FluentValidation;

namespace BusTracker.Application.Features.Permits.Queries.GetPendingVehiclePermits
{
    public class GetPendingVehiclePermitsQueryValidator : AbstractValidator<GetPendingVehiclePermitsQuery>
    {
        public GetPendingVehiclePermitsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.");
        }
    }
}
