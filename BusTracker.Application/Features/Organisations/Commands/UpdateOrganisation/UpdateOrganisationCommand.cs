using BusTracker.Application.Features.Organisations.DTOs;
using BusTracker.Domain.Enums;
using MediatR;

namespace BusTracker.Application.Features.Organisations.Commands.UpdateOrganisation
{
    public record UpdateOrganisationCommand(
        Guid OrganisationId,
        string? Name,
        string? Email,
        string? PhoneNumber
    //OrganizationType? Type
    ) : IRequest<OrganisationDto>;
}
