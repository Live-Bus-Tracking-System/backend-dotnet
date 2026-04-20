using MediatR;
using System;

namespace BusTracker.Application.Features.Organisations.Commands.VerifyOrgDeletionOtp
{
    public record VerifyOrgDeletionOtpCommand(Guid OrganisationId, string IntentId, string Otp) : IRequest<VerifyOrgDeletionOtpResult>;

    public record VerifyOrgDeletionOtpResult(string ConfirmToken);
}
