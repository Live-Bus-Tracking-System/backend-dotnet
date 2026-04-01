using BusTracker.Application.Common.Models;

namespace BusTracker.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(UserAuthDto user);

        (string RawToken, string TokenHash) GenerateRefreshToken();

        string HashRefreshToken(string rawToken);
    }
}
