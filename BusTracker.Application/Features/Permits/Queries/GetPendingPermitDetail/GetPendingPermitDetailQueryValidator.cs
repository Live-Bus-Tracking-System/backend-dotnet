using FluentValidation;

namespace BusTracker.Application.Features.Permits.Queries.GetPendingPermitDetail
{
    public class GetPendingPermitDetailQueryValidator : AbstractValidator<GetPendingPermitDetailQuery>
    {
        public GetPendingPermitDetailQueryValidator()
        {
            RuleFor(x => x.PermitId).NotEmpty();
        }
    }
}
