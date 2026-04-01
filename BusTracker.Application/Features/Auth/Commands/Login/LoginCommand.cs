using BusTracker.Application.Common.Models;
using MediatR;

namespace BusTracker.Application.Features.Auth.Commands.Login
{
    public record LoginCommand(
        string EmailOrPhone, 
        string Password, 
        string? IpAddress, 
        string? UserAgent) : IRequest<LoginResult>;

    public record LoginResult(UserAuthDto User, string AccessToken, string RefreshToken);
}
