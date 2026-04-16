using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Queries.GetOrganisationById
{
    public class GetOrganisationByIdQueryValidator : AbstractValidator<GetOrganisationByIdQuery>
    {
        public GetOrganisationByIdQueryValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty().WithMessage("Organisation ID is required.");
        }
    }
}
