using MediatR;

namespace BusTracker.Application.Features.Auth.Commands.ChangePassword
{
    public record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword) : IRequest;
}
