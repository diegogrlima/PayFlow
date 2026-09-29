using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);

        Task RevokeFamilyAsync(
            Guid familyId,
            string reason,
            CancellationToken cancellationToken = default);
    }
}
