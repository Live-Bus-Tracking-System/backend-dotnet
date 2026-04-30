using FluentValidation;

namespace BusTracker.Application.Features.Routes.Queries.SearchRoutes
{
    public class SearchRoutesQueryValidator : AbstractValidator<SearchRoutesQuery>
    {
        public SearchRoutesQueryValidator()
        {
            RuleFor(x => x.SearchTerm)
                .NotEmpty()
                .WithMessage("SearchTerm cannot be empty.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100);
        }
    }
}
