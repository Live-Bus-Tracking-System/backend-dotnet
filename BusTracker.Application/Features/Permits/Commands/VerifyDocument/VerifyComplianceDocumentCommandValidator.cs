using FluentValidation;

namespace BusTracker.Application.Features.Permits.Commands.VerifyDocument
{
    public class VerifyComplianceDocumentCommandValidator : AbstractValidator<VerifyComplianceDocumentCommand>
    {
        public VerifyComplianceDocumentCommandValidator()
        {
            RuleFor(x => x.PermitId).NotEmpty();
            RuleFor(x => x.DocumentId).NotEmpty();
        }
    }
}
