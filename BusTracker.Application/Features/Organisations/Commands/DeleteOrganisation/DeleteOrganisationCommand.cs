using MediatR;

namespace BusTracker.Application.Features.Organisations.Commands.DeleteOrganisation
{
    public record DeleteOrganisationCommand(Guid OrganisationId) : IRequest;
}
