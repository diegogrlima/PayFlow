using PayFlow.Entities;

namespace PayFlow.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Transaction>> GetHistoryByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default);
    }
}
