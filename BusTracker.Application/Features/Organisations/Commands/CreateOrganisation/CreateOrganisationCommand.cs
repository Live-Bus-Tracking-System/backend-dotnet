using BusTracker.Application.Features.Organisations.DTOs;
using BusTracker.Domain.Enums;
using MediatR;

namespace BusTracker.Application.Features.Organisations.Commands.CreateOrganisation
{
    public record CreateOrganisationCommand(
        string Name,
        string Email,
        string PhoneNumber,
        OrganizationType Type
    ) : IRequest<OrganisationDto>;
}
