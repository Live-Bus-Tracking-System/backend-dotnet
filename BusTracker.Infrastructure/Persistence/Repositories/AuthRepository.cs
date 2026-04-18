using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Infrastructure.Persistence.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AuthRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<RefreshToken?> GetActiveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.IsRevoked == false && t.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);
        }

        public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<IEnumerable<RefreshToken>> GetAllTokensForUserAsync(string userId, CancellationToken cancellationToken)
        {
            return await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task RevokeTokensAsync(IEnumerable<RefreshToken> tokens, CancellationToken cancellationToken)
        {
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAtUtc = DateTime.UtcNow;
            }
            _dbContext.RefreshTokens.UpdateRange(tokens);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
        {
            _dbContext.RefreshTokens.Add(token);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
        {
            _dbContext.RefreshTokens.Update(token);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RotateRefreshTokenAsync(RefreshToken toRevoke, RefreshToken toCreate, CancellationToken cancellationToken)
        {
            _dbContext.RefreshTokens.Update(toRevoke);
            _dbContext.RefreshTokens.Add(toCreate);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken)
        {
            var familyTokens = await _dbContext.RefreshTokens
                .Where(t => t.FamilyId == familyId && !t.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in familyTokens)
            {
                token.IsRevoked    = true;
                token.RevokedAtUtc = DateTime.UtcNow;
            }

            if (familyTokens.Count > 0)
                await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
