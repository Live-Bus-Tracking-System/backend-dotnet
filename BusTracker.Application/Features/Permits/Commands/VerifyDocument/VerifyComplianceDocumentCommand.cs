using MediatR;

namespace BusTracker.Application.Features.Permits.Commands.VerifyDocument
{
    public record VerifyComplianceDocumentCommand(
        Guid PermitId,
        Guid DocumentId,
        bool IsVerified
    ) : IRequest;
}
