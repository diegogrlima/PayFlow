using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Transaction>> GetHistoryByAccountIdAsync(
            Guid accountId,
            string? type,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default);
    }
}
