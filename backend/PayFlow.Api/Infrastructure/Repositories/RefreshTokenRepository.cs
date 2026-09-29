using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Infrastructure.Repositories
{
    public class RefreshTokenRepository(PayFlowDbContext dbContext)
        : IRefreshTokenRepository
    {
        public Task<RefreshToken?> GetByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return dbContext.RefreshTokens.SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
        }

        public async Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task RevokeFamilyAsync(
            Guid familyId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.RefreshTokens
                .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(token => token.RevokedAtUtc, now)
                        .SetProperty(token => token.RevocationReason, reason),
                    cancellationToken);
        }
    }
}
