using FluentValidation;

namespace BusTracker.Application.Features.Documents.Queries.GetUploadUrl
{
    public class GetUploadUrlQueryValidator : AbstractValidator<GetUploadUrlQuery>
    {
        public GetUploadUrlQueryValidator()
        {
            RuleFor(v => v.ContentType)
                .NotEmpty().WithMessage("ContentType is required.")
                .Must(BeAValidMimeType).WithMessage("Unsupported content type. Allowed types: application/pdf, image/jpeg, image/png.");

            RuleFor(v => v.Extension)
                .NotEmpty().WithMessage("Extension is required.");
        }

        private bool BeAValidMimeType(string contentType)
        {
            var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
            return allowedTypes.Contains(contentType.ToLower());
        }
    }
}
