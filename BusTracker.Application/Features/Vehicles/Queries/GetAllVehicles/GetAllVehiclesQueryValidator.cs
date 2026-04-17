using FluentValidation;

namespace BusTracker.Application.Features.Vehicles.Queries.GetAllVehicles
{
    public class GetAllVehiclesQueryValidator : AbstractValidator<GetAllVehiclesQuery>
    {
        public GetAllVehiclesQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1.")
                .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.");

            RuleFor(x => x.OrganisationId)
                .NotEqual(Guid.Empty).WithMessage("Organisation ID must not be an empty GUID.")
                .When(x => x.OrganisationId is not null);
        }
    }
}
