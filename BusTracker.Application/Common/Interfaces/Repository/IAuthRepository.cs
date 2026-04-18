using BusTracker.Domain.Entities;

namespace BusTracker.Application.Common.Interfaces.Repository
{
    public interface IAuthRepository
    {
        Task<RefreshToken?> GetActiveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
        Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

        Task<IEnumerable<RefreshToken>> GetAllTokensForUserAsync(string userId, CancellationToken cancellationToken);
        Task RevokeTokensAsync(IEnumerable<RefreshToken> tokens, CancellationToken cancellationToken);
        Task SaveRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);
        Task UpdateRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);
        Task RotateRefreshTokenAsync(RefreshToken toRevoke, RefreshToken toCreate, CancellationToken cancellationToken);
        Task RevokeTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken);
    }
}
