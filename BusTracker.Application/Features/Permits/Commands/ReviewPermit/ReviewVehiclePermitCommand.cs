using MediatR;

namespace BusTracker.Application.Features.Permits.Commands.ReviewPermit
{
    public record ReviewVehiclePermitCommand(
        Guid PermitId,
        bool IsApproved,
        string? RejectionReason,
        Guid? RouteId
    ) : IRequest;
}
