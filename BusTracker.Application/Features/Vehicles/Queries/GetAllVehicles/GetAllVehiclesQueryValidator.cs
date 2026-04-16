using FluentValidation;

namespace BusTracker.Application.Features.Vehicles.Queries.GetAllVehicles
{
    public class GetAllVehiclesQueryValidator : AbstractValidator<GetAllVehiclesQuery>
    {
        public GetAllVehiclesQueryValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEqual(Guid.Empty).WithMessage("Organisation ID must not be an empty GUID.")
                .When(x => x.OrganisationId is not null);
        }
    }
}
