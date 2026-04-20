using MediatR;
using System;

namespace BusTracker.Application.Features.Organisations.Commands.InitiateOrgDeletion
{
    public record InitiateOrgDeletionCommand(Guid OrganisationId, string Password) : IRequest<InitiateOrgDeletionResult>;

    public record InitiateOrgDeletionResult(string IntentId);
}
