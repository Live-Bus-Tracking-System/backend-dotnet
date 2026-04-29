using FluentValidation;

namespace BusTracker.Application.Features.Documents.Queries.GetDocumentViewUrl
{
    public class GetDocumentViewUrlQueryValidator : AbstractValidator<GetDocumentViewUrlQuery>
    {
        public GetDocumentViewUrlQueryValidator()
        {
            RuleFor(v => v.DocumentId).NotEmpty().WithMessage("DocumentId is required.");
        }
    }
}
