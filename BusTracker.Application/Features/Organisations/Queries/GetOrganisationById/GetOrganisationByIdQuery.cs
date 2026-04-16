using BusTracker.Application.Features.Organisations.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Organisations.Queries.GetOrganisationById
{
    public record GetOrganisationByIdQuery(Guid OrganisationId) : IRequest<OrganisationDto>;
}
