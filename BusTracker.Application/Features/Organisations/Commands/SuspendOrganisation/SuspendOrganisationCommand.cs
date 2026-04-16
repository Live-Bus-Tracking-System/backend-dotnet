using MediatR;

namespace BusTracker.Application.Features.Organisations.Commands.SuspendOrganisation
{
    public record SuspendOrganisationCommand(Guid OrganisationId, string? Reason) : IRequest;
}
