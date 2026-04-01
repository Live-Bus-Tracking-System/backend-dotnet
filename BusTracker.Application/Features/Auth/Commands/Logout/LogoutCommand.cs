using MediatR;

namespace BusTracker.Application.Features.Auth.Commands.Logout
{
    public record LogoutCommand(string RawRefreshToken) : IRequest;
}
