using FluentValidation;

namespace BusTracker.Application.Features.Organisations.Queries.GetAllOrganisations
{
    public class GetAllOrganisationsQueryValidator : AbstractValidator<GetAllOrganisationsQuery>
    {
        public GetAllOrganisationsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1.")
                .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.");

            RuleFor(x => x.Status)
                .IsInEnum().When(x => x.Status.HasValue).WithMessage("Status must be a valid enum value.");

            RuleFor(x => x.Type)
                .IsInEnum().When(x => x.Type.HasValue).WithMessage("Type must be a valid enum value.");
        }
    }
}
