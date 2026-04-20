using MediatR;
using System;

namespace BusTracker.Application.Features.Organisations.Commands.ConfirmOrgDeletion
{
    public record ConfirmOrgDeletionCommand(Guid OrganisationId, string ConfirmToken) : IRequest;
}
