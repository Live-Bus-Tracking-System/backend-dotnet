using BusTracker.Application.Features.Auth.Commands.Login;
using MediatR;

namespace BusTracker.Application.Features.Auth.Commands.Register
{
    public record RegisterCommand(
        string FullName,
        string? Email,
        string PhoneNumber,
        string Password,
        string? IpAddress,
        string? UserAgent) : IRequest<LoginResult>;
}
