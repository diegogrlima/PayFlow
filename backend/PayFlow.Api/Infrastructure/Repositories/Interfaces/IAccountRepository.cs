using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Account account,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Account account,
            CancellationToken cancellationToken = default);
    }
}
