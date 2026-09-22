using Microsoft.EntityFrameworkCore;
using PayFlow.Data;
using PayFlow.Entities;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Repositories
{
    public class TransactionRepository(PayFlowDbContext dbContext) : ITransactionRepository
    {
        public Task<Transaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Transactions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    transaction => transaction.Id == id,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Transaction>> GetHistoryByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.SourceAccountId == accountId ||
                    transaction.DestinationAccountId == accountId)
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default)
        {
            await dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
