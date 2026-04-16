using MediatR;

namespace BusTracker.Application.Features.Organisations.Commands.ActivateOrganisation
{
    public record ActivateOrganisationCommand(Guid OrganisationId) : IRequest;
}
