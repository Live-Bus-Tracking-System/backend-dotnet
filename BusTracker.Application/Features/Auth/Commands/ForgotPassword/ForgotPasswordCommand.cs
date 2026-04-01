using MediatR;

namespace BusTracker.Application.Features.Auth.Commands.ForgotPassword
{
    public record ForgotPasswordCommand(string EmailOrPhone) : IRequest<string>;
}
