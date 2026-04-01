using MediatR;

namespace BusTracker.Application.Features.Auth.Commands.ResetPassword
{
    public record ResetPasswordCommand(string EmailOrPhone, string Token, string NewPassword) : IRequest;
}
